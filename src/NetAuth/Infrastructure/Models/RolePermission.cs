using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Models;

public sealed class RolePermission
{
    public required RoleId RoleId { get; init; }
    public required PermissionId PermissionId { get; init; }
}