using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.Users.Login;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

[UsedImplicitly]
internal sealed class LoginEndpoint : IEndpoint
{
    public sealed record Request(
        string Email,
        string Password,
        string DeviceId
    );

    public sealed record Response(
        string AccessToken,
        string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(
                    Email: request.Email,
                    Password: request.Password,
                    DeviceId: request.DeviceId);

                var either = await sender.Send(command, cancellationToken);

                return either
                    .Select(result => new Response(
                        AccessToken: result.AccessToken,
                        RefreshToken: result.RefreshToken))
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("Login")
            .WithSummary("Authenticate user with email & password.")
            .WithDescription("Returns JWT access token and refresh token when credentials are valid.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithTags(Tags.Authentication)
            .RequireRateLimiting(RateLimiterPolicyNames.LoginLimiter);
    }
}