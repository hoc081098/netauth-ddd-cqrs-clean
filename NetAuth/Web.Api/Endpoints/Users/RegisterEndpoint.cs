using MediatR;
using NetAuth.Application.Users.Register;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

internal sealed class RegisterEndpoint : IEndpoint
{
    public sealed record Request(
        string Email,
        string Username,
        string Password);

    public sealed record Response(string AccessToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            Request request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(
                Email: request.Email,
                Username: request.Username,
                Password: request.Password
            );

            var either = await sender.Send(command, cancellationToken);

            return either
                .Select(result => new Response(AccessToken: result.AccessToken))
                .Match(Right: Results.Ok, Left: CustomResults.Err);
        });
    }
}