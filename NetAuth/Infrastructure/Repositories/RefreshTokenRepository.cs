using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext dbContext) :
    GenericRepository<Guid, RefreshToken>(dbContext),
    IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public Task<int> DeleteExpiredByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Where(RefreshTokenExpressions.IsExpired(userId, currentUtc))
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetNonExpiredActiveTokensByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .Where(RefreshTokenExpressions.IsValid(userId, currentUtc))
            .ToListAsync(cancellationToken);
}