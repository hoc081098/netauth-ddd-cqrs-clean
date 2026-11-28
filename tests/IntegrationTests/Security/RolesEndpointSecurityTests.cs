using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetAuth.Application.Users.Login;
using NetAuth.Application.Users.Register;
using NetAuth.Application.Users.SetUserRoles;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;
using NetAuth.Infrastructure;
using NetAuth.Infrastructure.Authentication;
using NetAuth.Infrastructure.Outbox;
using NetAuth.IntegrationTests.Infrastructure;
using NetAuth.TestUtils;
using NetAuth.Web.Api.Contracts;
using NetAuth.Web.Api.Endpoints.Users;
using MediatR;
using System.Text.Json;

namespace NetAuth.IntegrationTests.Security;

/// <summary>
/// Exercises <c>GET /v1/roles</c> (requires <c>permission:roles:read</c>) through real HTTP,
/// JWT validation, claims transformation and authorization.
/// </summary>
public class RolesEndpointSecurityTests(HttpTestWebAppFactory factory) : IClassFixture<HttpTestWebAppFactory>
{
    private static readonly Uri RolesUrl = new("/v1/roles", UriKind.Relative);
    private const string Password = "Password123!";

    [Fact]
    public async Task GetRoles_WithoutToken_Returns401()
    {
        using var client = factory.CreateClient();

        using var response = await GetRolesAsync(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithMalformedToken_Returns401()
    {
        using var response = await GetRolesAsync("not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithTokenSignedByWrongKey_Returns401()
    {
        var token = CreateToken(
            userId: Guid.NewGuid(),
            signingKey: "another-secret-key-another-secret-key-1234567890",
            expires: DateTime.UtcNow.AddMinutes(5));

        using var response = await GetRolesAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithExpiredToken_Returns401()
    {
        var token = CreateToken(
            userId: Guid.NewGuid(),
            signingKey: GetJwtConfig().SecretKey,
            expires: DateTime.UtcNow.AddMinutes(-5));

        using var response = await GetRolesAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithValidTokenWithoutPermission_Returns403()
    {
        var (_, accessToken) = await RegisterAndLoginAsync();

        using var response = await GetRolesAsync(accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithValidTokenWithPermission_Returns200()
    {
        var (userId, accessToken) = await RegisterAndLoginAsync();
        await SetRolesAndWaitForOutboxAsync(userId, Role.Administrator, Role.Member);

        using var response = await GetRolesAsync(accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetRolesEndpoint.Response>();
        Assert.NotNull(body);
        Assert.Equivalent(
            new[]
            {
                new RoleResponse(Role.Administrator.Id.Value, Role.Administrator.Name),
                new RoleResponse(Role.Member.Id.Value, Role.Member.Name)
            },
            body.Roles);
    }

    [Fact]
    public async Task GetRoles_AfterRevokingRole_SameTokenReturns403()
    {
        var (userId, accessToken) = await RegisterAndLoginAsync();
        await SetRolesAndWaitForOutboxAsync(userId, Role.Administrator, Role.Member);

        // Populates the permission cache with the administrator permissions.
        using (var allowed = await GetRolesAsync(accessToken))
        {
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        // Revoke the administrator role, which will trigger a UserRolesChangedDomainEvent
        // and invalidate the permission cache.
        await SetRolesAndWaitForOutboxAsync(userId, Role.Member);

        // The same token should now return 403 Forbidden, because the `permission:roles:read` permission was revoked.
        using var denied = await GetRolesAsync(accessToken);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private async Task<HttpResponseMessage> GetRolesAsync(string? accessToken)
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, RolesUrl);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    private JwtConfig GetJwtConfig() => factory.Services.GetRequiredService<IOptions<JwtConfig>>().Value;

    private string CreateToken(Guid userId, string signingKey, DateTime expires)
    {
        var jwtConfig = GetJwtConfig();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtConfig.Issuer,
            audience: jwtConfig.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            notBefore: expires.AddMinutes(-10),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Registers a unique Member user and logs in through the real use cases.</summary>
    private async Task<(Guid UserId, string AccessToken)> RegisterAndLoginAsync()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var unique = Guid.NewGuid().ToString("N");
        var email = $"sec_{unique}@example.com";

        var userId = (await sender.Send(new RegisterCommand($"sec_{unique}", email, Password)))
            .RightValueOrThrow().UserId;
        var login = (await sender.Send(new LoginCommand(email, Password, Guid.NewGuid())))
            .RightValueOrThrow();

        return (userId, login.AccessToken);
    }

    /// <summary>
    /// Sets roles via the real use case, then waits until the outbox message for that change
    /// is processed (which includes the permission cache invalidation).
    /// </summary>
    private async Task SetRolesAndWaitForOutboxAsync(Guid userId, params Role[] roles)
    {
        // Set roles via the real use case, which will trigger a UserRolesChangedDomainEvent
        using (var scope = factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new SetUserRolesCommand(
                userId,
                [.. roles.Select(r => r.Id.Value)],
                RoleChangeActor.System));
            result.ShouldBeRight();
        }

        var userIdJson = JsonSerializer.Serialize(new { UserId = userId });
        var eventType = typeof(UserRolesChangedDomainEvent).FullName;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Poll until the outbox message is processed, which includes the permission cache invalidation.
        while (true)
        {
            using var scope = factory.Services.CreateScope();

            // Drive the processor explicitly; the background Quartz job may be racing us for the same rows,
            // so we then poll for completion instead of assuming this call handled the message.
            await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().Execute(cts.Token);

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var matchedOutboxMessages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(m => m.Type == eventType &&
                            EF.Functions.JsonContains(m.Content, userIdJson))
                .OrderByDescending(m => m.OccurredOnUtc)
                .ToArrayAsync(cts.Token);

            var hasProcessedMessage = matchedOutboxMessages.Any(m =>
                m is { ProcessedOnUtc: not null, Error: null });
            if (hasProcessedMessage)
            {
                return;
            }

            // A failed publish keeps ProcessedOnUtc null (for retry) but records the error and bumps AttemptCount;
            // only deserialization failures set ProcessedOnUtc together with Error.
            var hasFailedMessage = matchedOutboxMessages.Any(m => m.Error is not null);
            if (hasFailedMessage)
            {
                Assert.Fail(
                    $"Outbox message for {eventType} (UserId: {userId}) failed to process. " +
                    $"Check the logs for details.");
            }

            await Task.Delay(100, cts.Token);
        }
    }
}