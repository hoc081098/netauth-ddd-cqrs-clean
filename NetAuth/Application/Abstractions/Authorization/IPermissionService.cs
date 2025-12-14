namespace NetAuth.Application.Abstractions.Authorization;

/// <summary>
/// Service to manage user permissions
/// </summary>
internal interface IPermissionService
{
    /// <summary>
    /// Fetch permissions from database, then cache
    /// IMPORTANT: Cache these results to avoid DB hits on every request
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cached permissions for user.
    /// Call this when user roles/permissions are modified.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task InvalidatePermissionsCacheAsync(Guid userId, CancellationToken cancellationToken = default);
}