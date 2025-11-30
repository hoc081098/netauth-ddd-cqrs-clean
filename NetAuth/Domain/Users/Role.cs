using JetBrains.Annotations;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public record struct RoleId(int Value)
{
    internal static readonly RoleId AdministratorId = new(1);
    internal static readonly RoleId MemberId = new(2);
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

    public ICollection<Permission> Permissions { get; } = [];
}