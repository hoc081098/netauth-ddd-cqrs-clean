namespace NetAuth.Domain.Users;

public sealed class Role
{
    public static readonly Role Administrator = new(name: "Administrator");
    public static readonly Role Member = new(name: "Member");

    // ReSharper disable once UnusedMember.Local
    /// <remarks>Required by EF Core.</remarks>
    private Role()
    {
    }

    private Role(string name) => Name = name;

    public string Name { get; } = null!;
}