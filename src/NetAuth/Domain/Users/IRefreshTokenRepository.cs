namespace NetAuth.Domain.Users;

/// <summary>
/// Repository interface for refresh token operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its token hash value.
    /// </summary>
    /// <param name="tokenHash">The token hash value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refresh token if found; otherwise, null.</returns>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    void Insert(RefreshToken refreshToken);

    /// <summary>
    /// Removes all expired refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentUtc">The current UTC time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<int> DeleteExpiredByUserIdAsync(Guid userId, DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all non-expired active refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentUtc">The current UTC time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task<IReadOnlyList<RefreshToken>> GetNonExpiredActiveTokensByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default);
}