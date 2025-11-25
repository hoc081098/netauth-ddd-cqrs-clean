namespace NetAuth.Domain.Users;

public sealed class RolePermission
{
    public required RoleId RoleId { get; init; }
    public required PermissionId PermissionId { get; init; }
}