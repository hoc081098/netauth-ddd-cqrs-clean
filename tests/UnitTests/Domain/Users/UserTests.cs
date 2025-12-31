using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.UnitTests.Domain.Users;


public class UserTests : BaseTest
{
    [Fact]
    public void CreateUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing

        // Act
        var user = User.Create(email, username, passwordHash);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Contains(Role.Member, user.Roles);

        var userCreatedDomainEvent = AssertDomainEventWasPublished<UserCreatedDomainEvent>(user);
        Assert.Equal(user.Id, userCreatedDomainEvent.UserId);
    }

    [Fact]
    public void VerifyPasswordHash_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var password = UserTestData.PlainPassword;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing

        var user = User.Create(email, username, passwordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(password: password,
            passwordHashChecker: passwordHashChecker);

        // Assert
        Assert.True(isMatch);
    }

    [Fact]
    public void VerifyPasswordHash_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var password = UserTestData.PlainPassword;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing
        var user = User.Create(email, username, passwordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(
            password: password + "Wrong",
            passwordHashChecker: passwordHashChecker);

        // Assert
        Assert.False(isMatch);
    }

    [Fact]
    public void Roles_ShouldBeReadOnly()
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);

        // Act & Assert
        Assert.IsType<IReadOnlyCollection<Role>>(user.Roles, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() =>
            ((ICollection<Role>)user.Roles).Add(Role.Administrator));
    }

    [Theory]
    [MemberData(
        memberName: nameof(UserTestData.InvalidRoles),
        MemberType = typeof(UserTestData))]
    public void SetRoles_WithNullOrEmptyRoles_ShouldReturnDomainError(IReadOnlyList<Role>? roles)
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);

        // Act
        var result = user.SetRoles(roles: roles!, actor: RoleChangeActor.System);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.EmptyRolesNotAllowed, left));
    }

    [Fact]
    public void SetRoles_WithTheCurrentRoles_ShouldReturnSuccessButMakeNoChanges()
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);
        var currentRoles = user.Roles;

        // Act
        var result = user.SetRoles(roles: currentRoles, actor: RoleChangeActor.System);

        // Assert
        result.ShouldBeRight();
        Assert.Equal(currentRoles, user.Roles);
    }

    [Theory]
    [InlineData(RoleChangeActor.Privileged)]
    [InlineData(RoleChangeActor.System)]
    public void SetRoles_WithNewValidRolesAndValidActor_ShouldReturnSuccess(RoleChangeActor actor)
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);
        var oldRoles = user.Roles;
        var newRoles = new List<Role> { Role.Administrator, Role.Member };

        // Act
        var result = user.SetRoles(newRoles, actor);

        // Assert
        result.ShouldBeRight();
        Assert.Equal(newRoles, user.Roles);

        var roleChangedDomainEvent = AssertDomainEventWasPublished<UserRolesChangedDomainEvent>(user);
        Assert.Equal(user.Id, roleChangedDomainEvent.UserId);
        Assert.True(
            roleChangedDomainEvent.OldRoleIds
                .SetEquals(oldRoles.Select(r => r.Id)));
        Assert.True(
            roleChangedDomainEvent.NewRoleIds
                .SetEquals(newRoles.Select(r => r.Id)));
    }

    [Fact]
    public void SetRoles_UserActorModifyingOwnAdminRoles_ShouldReturnDomainError()
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);
        // Make user an admin
        user.SetRoles([Role.Administrator], RoleChangeActor.System).ShouldBeRight();

        var newRoles = new List<Role> { Role.Member };

        // Act
        var result = user.SetRoles(newRoles, RoleChangeActor.User);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.CannotModifyOwnAdminRoles, left));
    }

    [Fact]
    public void SetRoles_UserActorGrantingAdminRole_ShouldReturnDomainError()
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);

        // Act
        var result = user.SetRoles([Role.Administrator], RoleChangeActor.User);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.CannotGrantAdminRole, left));
    }
}