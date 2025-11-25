namespace NetAuth.Domain.Users;

public sealed class Permission
{
    public static readonly Permission GetUsers = new(code: "users:read");
    public static readonly Permission ModifyUser = new(code: "users:update");

    public Permission(string code)
    {
        Code = code;
    }

    public string Code { get; }
}