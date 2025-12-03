using JetBrains.Annotations;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public readonly record struct PermissionId(int Value) : IComparable<PermissionId>
{
    internal static readonly PermissionId GetUsersId = new(1);
    internal static readonly PermissionId ModifyUserId = new(2);

    public int CompareTo(PermissionId other) => Value.CompareTo(other.Value);

    public static bool operator <(PermissionId left, PermissionId right) => left.CompareTo(right) < 0;
    public static bool operator >(PermissionId left, PermissionId right) => left.CompareTo(right) > 0;
    public static bool operator <=(PermissionId left, PermissionId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PermissionId left, PermissionId right) => left.CompareTo(right) >= 0;
}

public sealed class Permission : Entity<PermissionId>
{
    public static readonly Permission GetUsers = new(PermissionId.GetUsersId, "users:read");
    public static readonly Permission ModifyUser = new(PermissionId.ModifyUserId, "users:update");

    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private Permission()
    {
    }

    private Permission(PermissionId id, string code) : base(id) => Code = code;

    public string Code { get; } = null!;
}