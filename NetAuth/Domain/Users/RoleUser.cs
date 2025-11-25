namespace NetAuth.Domain.Users;

public sealed class RoleUser
{
    public required RoleId RoleId { get; init; }
    public required Guid UserId { get; init; }
}