using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.Users.LoginWithRefreshToken;
using NetAuth.Web.Api.Contracts;
using NetAuth.Web.Api.OpenApi;

namespace NetAuth.Web.Api.Endpoints.Users;

[UsedImplicitly]
internal sealed class RefreshTokenEndpoint : IEndpoint
{
    [SwaggerRequired]
    public sealed record Request(
        string RefreshToken,
        Guid DeviceId);

    public sealed record Response(
        string AccessToken,
        string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh-token", async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginWithRefreshTokenCommand(
                    RefreshToken: request.RefreshToken,
                    DeviceId: request.DeviceId
                );

                var either = await sender.Send(command, cancellationToken);

                return either
                    .Select(result => new Response(
                        AccessToken: result.AccessToken,
                        RefreshToken: result.RefreshToken))
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("RefreshToken")
            .WithSummary("Refresh JWT access token using refresh token.")
            .WithDescription("Returns new JWT access token and refresh token when the provided refresh token is valid.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithTags(Tags.Authentication)
            .RequireRateLimiting(RateLimiterPolicyNames.RefreshTokenLimiter);
    }
}