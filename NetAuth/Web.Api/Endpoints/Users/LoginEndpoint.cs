using MediatR;
using NetAuth.Application.Users.Login;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

internal sealed class LoginEndpoint : IEndpoint
{
    public sealed record Request(string Email, string Password);

    public sealed record Response(string AccessToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(
                    Email: request.Email,
                    Password: request.Password);

                var either = await sender.Send(command, cancellationToken);

                return either
                    .Select(result => new Response(AccessToken: result.AccessToken))
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("Login")
            .WithSummary("Authenticate user with email & password.")
            .WithDescription("Returns JWT access token when credentials are valid.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}