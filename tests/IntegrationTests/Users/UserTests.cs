using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Users.Login;
using NetAuth.Application.Users.Register;
using NetAuth.Domain.Users;
using NetAuth.IntegrationTests.Infrastructure;
using NetAuth.TestUtils;
using Xunit.Abstractions;

namespace NetAuth.IntegrationTests.Users;

[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
public class UserTests(IntegrationTestWebAppFactory webAppFactory, ITestOutputHelper testOutputHelper)
    : BaseIntegrationTest(webAppFactory, testOutputHelper)
{
    #region Register Tests

    [Fact]
    public async Task Register_ShouldCreateNewUserAndStoreOutboxMessage()
    {
        // Arrange
        var registerCommand = new RegisterCommand(
            Username: "hoc081098",
            Email: "hoc081098@gmail.com",
            Password: "123456Aa@"
        );

        // Act
        var result = await Sender.Send(registerCommand);

        // Assert
        result.ShouldBeRight();

        // Single user with the email should exist
        var createdUser = await DbContext
            .Users
            .SingleAsync(u => u.Email.Value == registerCommand.Email);

        // Verify outbox message was created in the same transaction
        // ANOTHER WAY: var userIdJson = $$"""{"UserId":"{{createdUser.Id}}"}""";
        var userIdJson = JsonSerializer.Serialize(new { UserId = createdUser.Id });
        var outboxMessage = await DbContext.OutboxMessages
            .Where(om => EF.Functions.JsonContains(om.Content, userIdJson))
            .SingleAsync();

        Assert.Null(outboxMessage.ProcessedOnUtc); // Not processed yet
        Assert.Equal(0, outboxMessage.AttemptCount);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        const string email = "hoc081098_01@gmail.com";

        var registerCommand1 = new RegisterCommand(
            Username: "hoc081098_01",
            Email: email,
            Password: "123456Aa@");
        var registerCommand2 = new RegisterCommand(
            Username: "another-username",
            Email: email,
            Password: "123456Aa@");

        // Act
        var result1 = await Sender.Send(registerCommand1);
        var result2 = await Sender.Send(registerCommand2);

        // Assert
        result1.ShouldBeRight();
        result2.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.DuplicateEmail, left));

        var count = await DbContext
            .Users
            .CountAsync(u => u.Email.Value == registerCommand1.Email);
        Assert.Equal(1, count);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldCreateRefreshToken()
    {
        // Arrange
        const string email = "login_test@example.com";
        const string password = "Password123!";
        var deviceId = Guid.NewGuid();

        // First, register a user
        var registerResult = await Sender.Send(
            new RegisterCommand(
                Username: "login_test",
                Email: email,
                Password: password));
        var userId = registerResult.RightValueOrThrow().UserId;

        var loginCommand = new LoginCommand(
            Email: email,
            Password: password,
            DeviceId: deviceId);

        // Act
        var result = await Sender.Send(loginCommand);

        // Assert
        result.ShouldBeRight(r =>
        {
            Assert.NotEmpty(r.AccessToken);
            Assert.NotEmpty(r.RefreshToken);
        });

        // Verify refresh token was persisted in database
        var refreshToken = await DbContext
            .RefreshTokens
            .SingleAsync(rt => rt.DeviceId == deviceId);
        Assert.NotEmpty(refreshToken.TokenHash);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.True(refreshToken.IsValid(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        const string email = "invalid_creds@example.com";

        // First, register a user
        var registerResult = await Sender.Send(
            new RegisterCommand(
                Username: "invalid_creds_user",
                Email: email,
                Password: "CorrectPassword123!"
            )
        );
        var userId = registerResult.RightValueOrThrow().UserId;

        var refreshTokenCountBefore = await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == userId);

        var loginCommand = new LoginCommand(
            Email: email,
            Password: "WrongPassword123!",
            DeviceId: Guid.NewGuid()
        );

        // Act
        var result = await Sender.Send(loginCommand);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.InvalidCredentials, left));

        // Verify no new refresh token was created
        var refreshTokenCountAfter = await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == userId);
        Assert.Equal(refreshTokenCountBefore, refreshTokenCountAfter);
    }

    #endregion

    //
    // #region LoginWithRefreshToken Tests
    //
    // [Fact]
    // public async Task LoginWithRefreshToken_ShouldRotateRefreshToken()
    // {
    //     // Arrange
    //     const string email = "refresh_test@example.com";
    //     const string password = "Password123!";
    //     var deviceId = Guid.NewGuid();
    //
    //     // Register and login to get initial refresh token
    //     await Sender.Send(new RegisterCommand(
    //         Username: "refresh_test",
    //         Email: email,
    //         Password: password
    //     ));
    //
    //     var loginResult = await Sender.Send(new LoginCommand(
    //         Email: email,
    //         Password: password,
    //         DeviceId: deviceId
    //     ));
    //
    //     var initialRefreshToken = loginResult.Match(
    //         Right: r => r.RefreshToken,
    //         Left: _ => throw new InvalidOperationException("Login failed")
    //     );
    //     var initialRefreshTokenCount = await DbContext.RefreshTokens.CountAsync();
    //
    //     // Act
    //     var refreshResult = await Sender.Send(new LoginWithRefreshTokenCommand(
    //         RefreshToken: initialRefreshToken,
    //         DeviceId: deviceId
    //     ));
    //
    //     // Assert
    //     refreshResult.ShouldBeRight(r =>
    //     {
    //         Assert.NotEmpty(r.AccessToken);
    //         Assert.NotEmpty(r.RefreshToken);
    //         Assert.NotEqual(initialRefreshToken, r.RefreshToken); // Token should be rotated
    //     });
    //
    //     // Verify old refresh token was invalidated and new one was created
    //     var finalRefreshTokenCount = await DbContext.RefreshTokens.CountAsync();
    //     Assert.Equal(initialRefreshTokenCount,
    //         finalRefreshTokenCount); // Count should remain the same (old deleted, new created)
    // }
    //
    // [Fact]
    // public async Task LoginWithRefreshToken_WithInvalidToken_ShouldFail()
    // {
    //     // Arrange
    //     var loginWithRefreshTokenCommand = new LoginWithRefreshTokenCommand(
    //         RefreshToken: "invalid_token_here",
    //         DeviceId: Guid.NewGuid()
    //     );
    //
    //     // Act
    //     var result = await Sender.Send(loginWithRefreshTokenCommand);
    //
    //     // Assert
    //     result.ShouldBeLeft(left =>
    //         Assert.Equal(UsersDomainErrors.RefreshToken.Invalid, left));
    // }
    //
    // #endregion
    //
    // #region SetUserRoles Tests
    //
    // [Fact]
    // public async Task SetUserRoles_ShouldUpdateRolesAndCreateOutboxMessage()
    // {
    //     // Arrange
    //     const string email = "roles_test@example.com";
    //
    //     // Register a user (will have Member role by default)
    //     await Sender.Send(new RegisterCommand(
    //         Username: "roles_test",
    //         Email: email,
    //         Password: "Password123!"
    //     ));
    //
    //     var user = await DbContext
    //         .Users
    //         .Include(u => u.Roles)
    //         .FirstAsync(u => u.Email.Value == email);
    //
    //     Assert.Single(user.Roles);
    //     Assert.Equal(Role.Member.Id, user.Roles[0].Id);
    //
    //     var setRolesCommand = new SetUserRolesCommand(
    //         UserId: user.Id,
    //         RoleIds: [Role.Administrator.Id.Value, Role.Member.Id.Value],
    //         RoleChangeActor: RoleChangeActor.System
    //     );
    //
    //     var outboxCountBefore = await DbContext.Set<OutboxMessage>().CountAsync();
    //
    //     // Act
    //     var result = await Sender.Send(setRolesCommand);
    //
    //     // Assert
    //     result.ShouldBeRight();
    //
    //     // Verify roles were updated in database
    //     var updatedUser = await DbContext
    //         .Users
    //         .Include(u => u.Roles)
    //         .FirstAsync(u => u.Id == user.Id);
    //
    //     Assert.Equal(2, updatedUser.Roles.Count);
    //     Assert.Contains(updatedUser.Roles, r => r.Id == Role.Administrator.Id);
    //     Assert.Contains(updatedUser.Roles, r => r.Id == Role.Member.Id);
    //
    //     // Verify outbox message was created for UserRolesChangedDomainEvent
    //     var outboxCountAfter = await DbContext.Set<OutboxMessage>().CountAsync();
    //     Assert.True(outboxCountAfter > outboxCountBefore, "New outbox message should be created");
    //
    //     // Verify the UserRolesChangedDomainEvent was created
    //     var rolesChangedEvents = await DbContext
    //         .Set<OutboxMessage>()
    //         .Where(m => m.Type == "NetAuth.Domain.Users.DomainEvents.UserRolesChangedDomainEvent")
    //         .OrderByDescending(m => m.OccurredOnUtc)
    //         .ToListAsync();
    //
    //     var rolesChangedEvent =
    //         rolesChangedEvents.FirstOrDefault(m => m.Content.Contains(user.Id.ToString(), StringComparison.Ordinal));
    //     Assert.NotNull(rolesChangedEvent);
    //     Assert.Null(rolesChangedEvent.ProcessedOnUtc);
    // }
    //
    // [Fact]
    // public async Task SetUserRoles_WithNonExistentRole_ShouldFail()
    // {
    //     // Arrange
    //     const string email = "roles_invalid@example.com";
    //
    //     await Sender.Send(new RegisterCommand(
    //         Username: "roles_invalid",
    //         Email: email,
    //         Password: "Password123!"
    //     ));
    //
    //     var user = await DbContext
    //         .Users
    //         .FirstAsync(u => u.Email.Value == email);
    //
    //     var setRolesCommand = new SetUserRolesCommand(
    //         UserId: user.Id,
    //         RoleIds: [999], // Non-existent role
    //         RoleChangeActor: RoleChangeActor.System
    //     );
    //
    //     // Act
    //     var result = await Sender.Send(setRolesCommand);
    //
    //     // Assert
    //     result.ShouldBeLeft(left =>
    //         Assert.Equal(UsersDomainErrors.User.OneOrMoreRolesNotFound, left));
    //
    //     // Verify roles were not changed
    //     var unchangedUser = await DbContext
    //         .Users
    //         .Include(u => u.Roles)
    //         .FirstAsync(u => u.Id == user.Id);
    //
    //     Assert.Single(unchangedUser.Roles);
    //     Assert.Equal(Role.Member.Id, unchangedUser.Roles[0].Id);
    // }
    //
    // [Fact]
    // public async Task SetUserRoles_WithEmptyRoles_ShouldFail()
    // {
    //     // Arrange
    //     const string email = "roles_empty@example.com";
    //
    //     await Sender.Send(new RegisterCommand(
    //         Username: "roles_empty",
    //         Email: email,
    //         Password: "Password123!"
    //     ));
    //
    //     var user = await DbContext
    //         .Users
    //         .FirstAsync(u => u.Email.Value == email);
    //
    //     var setRolesCommand = new SetUserRolesCommand(
    //         UserId: user.Id,
    //         RoleIds: [],
    //         RoleChangeActor: RoleChangeActor.System
    //     );
    //
    //     // Act
    //     var result = await Sender.Send(setRolesCommand);
    //
    //     // Assert - FluentValidation catches this before domain logic
    //     result.ShouldBeLeft(left =>
    //     {
    //         Assert.Equal("General.ValidationError", left.Code);
    //         Assert.Equal(DomainError.ErrorType.Validation, left.Type);
    //     });
    // }
    //
    // [Fact]
    // public async Task SetUserRoles_UserModifyingOwnAdminRole_ShouldFail()
    // {
    //     // Arrange
    //     const string email = "admin_self_modify@example.com";
    //
    //     await Sender.Send(new RegisterCommand(
    //         Username: "admin_self_modify",
    //         Email: email,
    //         Password: "Password123!"
    //     ));
    //
    //     var user = await DbContext
    //         .Users
    //         .Include(u => u.Roles)
    //         .FirstAsync(u => u.Email.Value == email);
    //
    //     // First, grant admin role using System actor
    //     await Sender.Send(new SetUserRolesCommand(
    //         UserId: user.Id,
    //         RoleIds: [Role.Administrator.Id.Value, Role.Member.Id.Value],
    //         RoleChangeActor: RoleChangeActor.System
    //     ));
    //
    //     // Now try to remove admin role as a regular user (not System/Privileged)
    //     var setRolesCommand = new SetUserRolesCommand(
    //         UserId: user.Id,
    //         RoleIds: [Role.Member.Id.Value], // Trying to remove Admin role
    //         RoleChangeActor: RoleChangeActor.User
    //     );
    //
    //     // Act
    //     var result = await Sender.Send(setRolesCommand);
    //
    //     // Assert
    //     result.ShouldBeLeft(left =>
    //         Assert.Equal(UsersDomainErrors.User.CannotModifyOwnAdminRoles, left));
    //
    //     // Verify roles remain unchanged
    //     var unchangedUser = await DbContext
    //         .Users
    //         .Include(u => u.Roles)
    //         .FirstAsync(u => u.Id == user.Id);
    //
    //     Assert.Equal(2, unchangedUser.Roles.Count);
    //     Assert.Contains(unchangedUser.Roles, r => r.Id == Role.Administrator.Id);
    // }
    //
    // #endregion
}