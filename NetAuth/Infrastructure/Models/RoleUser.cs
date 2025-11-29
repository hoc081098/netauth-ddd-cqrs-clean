using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Models;

public sealed class RoleUser
{
    public required RoleId RoleId { get; init; }
    public required Guid UserId { get; init; }
    
    // Navigation properties
    public Role Role { get; init; } = null!;
}