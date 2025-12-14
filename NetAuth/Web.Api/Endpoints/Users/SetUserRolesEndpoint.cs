using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Users.SetUserRoles;
using NetAuth.Domain.Users;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

[UsedImplicitly]
public class SetUserRolesEndpoint : IEndpoint
{
    [UsedImplicitly]
    public sealed record Request(IReadOnlyList<int> RoleIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/{id:guid}/roles", async (
                Guid id,
                Request request,
                ISender sender,
                IUserContext userContext,
                CancellationToken cancellationToken) =>
            {
                var actor = id == userContext.UserId
                    ? RoleChangeActor.User
                    : RoleChangeActor.Administrator;

                var command = new SetUserRolesCommand(
                    UserId: id,
                    RoleIds: request.RoleIds,
                    RoleChangeActor: actor
                );

                var result = await sender.Send(command, cancellationToken);

                return result
                    .Match(Right: _ => Results.NoContent(), Left: CustomResults.Err);
            })
            .WithName("SetUserRoles")
            .WithSummary("Set user roles")
            .WithDescription("Sets the roles for a specific user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(Tags.Authorization)
            .RequireAuthorization("permission:users:roles:update");
    }
}