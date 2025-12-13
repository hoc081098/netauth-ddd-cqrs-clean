using JetBrains.Annotations;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public readonly record struct PermissionId(int Value) : IComparable<PermissionId>
{
    // Users
    internal static readonly PermissionId GetUsersId = new(1);
    internal static readonly PermissionId ModifyUserId = new(2);
    
    // Todo Items
    internal static readonly PermissionId GetTodoItemsId = new(3);
    internal static readonly PermissionId CreateTodoItemId = new(4);
    internal static readonly PermissionId ModifyTodoItemId = new(5);
    
    // Roles
    internal static readonly PermissionId GetRolesId = new(6);

    public int CompareTo(PermissionId other) => Value.CompareTo(other.Value);

    public static bool operator <(PermissionId left, PermissionId right) => left.CompareTo(right) < 0;
    public static bool operator >(PermissionId left, PermissionId right) => left.CompareTo(right) > 0;
    public static bool operator <=(PermissionId left, PermissionId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(PermissionId left, PermissionId right) => left.CompareTo(right) >= 0;
}

public sealed class Permission : Entity<PermissionId>
{
    // Users
    public static readonly Permission GetUsers = new(PermissionId.GetUsersId, "users:read");
    public static readonly Permission ModifyUser = new(PermissionId.ModifyUserId, "users:update");
    
    // Todo Items
    public static readonly Permission GetTodoItems = new(PermissionId.GetTodoItemsId, "todo-items:read");
    public static readonly Permission CreateTodoItem = new(PermissionId.CreateTodoItemId, "todo-items:create");
    public static readonly Permission ModifyTodoItem = new(PermissionId.ModifyTodoItemId, "todo-items:update");

    // Roles
    public static readonly Permission GetRoles = new(PermissionId.GetRolesId, "roles:read");
    
    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private Permission()
    {
    }

    private Permission(PermissionId id, string code) : base(id) => Code = code;

    public string Code { get; } = null!;
}