using NetAuth.Domain.Users;

namespace NetAuth.Application.Users;

public sealed record RoleDto(
    RoleId Id,
    string Name
);

internal static class RoleDtoExtensions
{
    public static RoleDto ToRoleDto(this Role role) =>
        new(
            Id: role.Id,
            Name: role.Name);
}