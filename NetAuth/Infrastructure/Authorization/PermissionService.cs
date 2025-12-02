using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionService(
    AppDbContext dbContext,
    HybridCache cache,
    ILogger<PermissionService> logger
) : IPermissionService
{
    private const string CacheKeyPrefix = "auth:permissions:user:";

    private static readonly HybridCacheEntryOptions HybridCacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    private static string BuildCacheKey(Guid userId) => $"{CacheKeyPrefix}{userId:N}";

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        =>
            // NOTE: Must use `HashSet<string>` type argument here to ensure correct deserialization from cache.
            // Using interface type `IReadOnlySet<string>` will cause deserialization to fail.
            await cache.GetOrCreateAsync<HashSet<string>>(
                key: BuildCacheKey(userId),
                factory: async token => await QueryPermissionsAsync(userId, token),
                options: HybridCacheEntryOptions,
                cancellationToken: cancellationToken
            );

    public async Task InvalidatePermissionsCacheAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(BuildCacheKey(userId), cancellationToken);

    private Task<HashSet<string>> QueryPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.RoleUsers
            .AsNoTracking()
            .Where(ru => ru.UserId == userId)
            .SelectMany(ru => ru.Role.Permissions)
            .Select(p => p.Code)
            .Distinct()
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
}