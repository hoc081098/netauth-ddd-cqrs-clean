using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext dbContext) :
    GenericRepository<Guid, RefreshToken>(dbContext),
    IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == token, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .Where(rt => rt.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<int> DeleteExpiredByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Where(rt => rt.UserId == userId && rt.ExpiresOnUtc <= currentUtc)
            .ExecuteDeleteAsync(cancellationToken);
}