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

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .Where(rt => rt.UserId == userId && rt.Status == RefreshTokenStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<int> DeleteExpiredByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Where(rt => rt.UserId == userId && rt.ExpiresOnUtc <= currentUtc)
            .ExecuteDeleteAsync(cancellationToken);
}