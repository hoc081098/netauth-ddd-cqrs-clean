using NetAuth.Application.Users;

namespace NetAuth.Web.Api.Contracts;

public sealed record RoleResponse(
    int Id,
    string Name
);

internal static class RoleResponseExtensions
{
    public static RoleResponse ToRoleResponse(this RoleDto role) =>
        new(
            Id: role.Id.Value,
            Name: role.Name);
}