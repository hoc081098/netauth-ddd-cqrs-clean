using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.Users.GetRoles;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.Users;

[UsedImplicitly]
public class GetRolesEndpoint : IEndpoint
{
    [UsedImplicitly]
    public sealed record Response(IReadOnlyList<RoleResponse> Roles);

    [UsedImplicitly]
    public sealed record RoleResponse(
        int Id,
        string Name
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/roles", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetRolesQuery();

                var result = await sender.Send(query, cancellationToken);

                return result
                    .Map(ToResponse)
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("GetRoles")
            .WithSummary("Get roles")
            .WithDescription("Retrieves all roles.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithTags(Tags.Authorization)
            .RequireAuthorization("permission:roles:read");
    }

    private static Response ToResponse(GetRolesResult result)
    {
        var roles = result.Roles
            .Select(role => new RoleResponse(
                Id: role.Id.Value,
                Name: role.Name))
            .ToArray();

        return new Response(roles);
    }
}