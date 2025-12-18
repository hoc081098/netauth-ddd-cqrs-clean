using JetBrains.Annotations;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public readonly record struct RoleId(int Value) : IComparable<RoleId>
{
    internal static readonly RoleId AdministratorId = new(1);
    internal static readonly RoleId MemberId = new(2);

    public int CompareTo(RoleId other) => Value.CompareTo(other.Value);

    public static bool operator >(RoleId left, RoleId right) => left.CompareTo(right) > 0;
    public static bool operator <(RoleId left, RoleId right) => left.CompareTo(right) < 0;
    public static bool operator >=(RoleId left, RoleId right) => left.CompareTo(right) >= 0;
    public static bool operator <=(RoleId left, RoleId right) => left.CompareTo(right) <= 0;

    public bool IsAdministrator => this == AdministratorId;
}

public sealed class Role : Entity<RoleId>
{
    public static readonly Role Administrator = new(RoleId.AdministratorId, "Administrator");
    public static readonly Role Member = new(RoleId.MemberId, "Member");

    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private Role()
    {
    }

    private Role(RoleId id, string name) : base(id) => Name = name;

    public string Name { get; } = null!;

    // Navigation property
    public IReadOnlyList<Permission> Permissions { get; } = [];
}