using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class RoleRepository(AppDbContext dbContext) :
    GenericRepository<RoleId, Role>(dbContext),
    IRoleRepository
{
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await DbContext.RoleUsers
            .AsNoTracking()
            .Where(ru => ru.UserId == userId)
            .Select(ru => ru.Role)
            .Distinct()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(
        IReadOnlySet<RoleId> roleIds,
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
}