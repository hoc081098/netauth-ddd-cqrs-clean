using System.Text.Json;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionService(
    AppDbContext dbContext,
    IDistributedCache distributedCache
) : IPermissionService
{
    private const string CacheKeyPrefix = "user_permissions:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private static string BuildCacheKey(Guid userId) => $"{CacheKeyPrefix}{userId:N}";

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(userId);

        // 1. Try cache
        var cachedBytes = await distributedCache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes is not null)
        {
            var cachedList = JsonSerializer.Deserialize<List<string>>(cachedBytes);
            Guard.Against.Null(cachedList,
                exceptionCreator: () => new InvalidOperationException("Cached permissions deserialized to null"));

            return cachedList.ToHashSet(StringComparer.Ordinal);
        }

        // 2. Query DB
        var permissions = await dbContext.RoleUsers
            .AsNoTracking()
            .Where(ru => ru.UserId == userId)
            .SelectMany(ru => ru.Role.Permissions)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // 3. Save to cache
        var data = JsonSerializer.SerializeToUtf8Bytes(permissions);
        await distributedCache.SetAsync(
            cacheKey,
            data,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return permissions.ToHashSet(StringComparer.Ordinal);
    }
}