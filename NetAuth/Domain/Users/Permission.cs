namespace NetAuth.Domain.Users;

public sealed class Permission
{
    public static readonly Permission GetUsers = new("users:read");
    public static readonly Permission ModifyUser = new("users:update");

    public Permission(string code)
    {
        Code = code;
    }

    public string Code { get; }
}