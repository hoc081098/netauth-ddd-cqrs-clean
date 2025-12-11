namespace NetAuth.Domain.Users;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}