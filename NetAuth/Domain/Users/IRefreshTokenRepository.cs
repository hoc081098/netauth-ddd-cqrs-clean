namespace NetAuth.Domain.Users;

/// <summary>
/// Repository interface for refresh token operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its token value.
    /// </summary>
    /// <param name="token">The token value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refresh token if found; otherwise, null.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of refresh tokens.</returns>
    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    void Insert(RefreshToken refreshToken);

    /// <summary>
    /// Removes a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to remove.</param>
    void Remove(RefreshToken refreshToken);

    /// <summary>
    /// Removes all expired refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentUtc">The current UTC time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteExpiredByUserIdAsync(Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken = default);
}

