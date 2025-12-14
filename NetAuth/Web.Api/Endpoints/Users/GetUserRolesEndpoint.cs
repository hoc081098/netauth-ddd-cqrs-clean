using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.Users.GetUserRoles;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

[UsedImplicitly]
public class GetUserRolesEndpoint : IEndpoint
{
    [UsedImplicitly]
    public sealed record Response(IReadOnlyList<RoleResponse> Roles);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{id:guid}/roles", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserRolesQuery(UserId: id);

                var result = await sender.Send(query, cancellationToken);

                return result
                    .Map(ToResponse)
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("GetUserRoles")
            .WithSummary("Get user roles")
            .WithDescription("Retrieves all roles assigned to a specific user.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithTags(Tags.Authorization)
            .RequireAuthorization("permission:users:roles:read");
    }

    private static Response ToResponse(GetUserRolesResult result)
    {
        var roles = result.Roles
            .Select(role => role.ToRoleResponse())
            .ToArray();

        return new Response(roles);
    }
}