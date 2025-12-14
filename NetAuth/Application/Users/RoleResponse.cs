using NetAuth.Domain.Users;

namespace NetAuth.Application.Users;

public sealed record RoleResponse(
    RoleId Id,
    string Name
);

internal static class RoleMapper
{
    public static RoleResponse ToRoleResponse(this Role role) =>
        new(
            Id: role.Id,
            Name: role.Name);
}