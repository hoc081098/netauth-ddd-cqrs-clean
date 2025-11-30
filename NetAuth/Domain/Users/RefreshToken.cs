using System.Diagnostics.Contracts;
using Ardalis.GuardClauses;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

/// <summary>
/// Represents a refresh token used for JWT token renewal.
/// </summary>
public sealed class RefreshToken : Entity<Guid>, IAuditableEntity
{
    /// <summary>
    /// Gets the unique token value.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the expiration date and time in UTC.
    /// </summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>
    /// Gets the ID of the user who owns this refresh token.
    /// </summary>
    public Guid UserId { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; }

    public User User { get; } = null!;
    
    /// <remarks>Required by EF Core.</remarks>
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, string token, DateTimeOffset expiresOnUtc, Guid userId)
        : base(id)
    {
        Guard.Against.NullOrWhiteSpace(token);
        Guard.Against.Default(userId);

        Token = token;
        ExpiresOnUtc = expiresOnUtc;
        UserId = userId;
    }

    /// <summary>
    /// Gets a value indicating whether the refresh token is expired.
    /// </summary>
    [Pure]
    public bool IsExpired(DateTimeOffset currentUtc) => currentUtc >= ExpiresOnUtc;

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="token">The unique token value.</param>
    /// <param name="expiresOnUtc">The expiration date and time in UTC.</param>
    /// <param name="userId">The ID of the user who owns this token.</param>
    /// <returns>A new <see cref="RefreshToken"/> instance.</returns>
    [Pure]
    public static RefreshToken Create(string token, DateTimeOffset expiresOnUtc, Guid userId)
    {
        return new RefreshToken(
            id: Guid.CreateVersion7(),
            token: token,
            expiresOnUtc: expiresOnUtc,
            userId: userId);
    }

    /// <summary>
    /// Revokes the refresh token by setting its expiration to the current time.
    /// </summary>
    /// <param name="currentUtc">The current UTC time.</param>
    public void Revoke(DateTimeOffset currentUtc)
    {
        ExpiresOnUtc = currentUtc;
    }

    public void UpdateToken(string token, DateTimeOffset expiresOnUtc)
    {
        Guard.Against.NullOrWhiteSpace(token);

        Token = token;
        ExpiresOnUtc = expiresOnUtc;
    }
}