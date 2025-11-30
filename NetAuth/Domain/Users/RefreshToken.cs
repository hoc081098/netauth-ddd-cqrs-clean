using System.Linq.Expressions;
using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Domain.Users;

public enum RefreshTokenStatus
{
    Active = 0,
    Revoked = 1,
    Compromised = 2 // cả chain bị coi là compromised
}

/// <summary>
/// Represents a refresh token used for JWT token renewal.
/// </summary>
public sealed class RefreshToken : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Gets the unique token hash value.
    /// </summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>
    /// Gets the expiration date and time in UTC.
    /// </summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>
    /// Gets the ID of the user who owns this refresh token.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the device identifier associated with this refresh token.
    /// </summary>
    public string DeviceId { get; } = null!;

    /// <summary>
    /// The date and time when the refresh token was revoked, if applicable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// The status of the refresh token.
    /// </summary>
    public RefreshTokenStatus Status { get; private set; }

    /// <summary>
    /// The ID of the refresh token that replaced this one, if applicable.
    /// </summary>
    public Guid? ReplacedById { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; }

    /// <summary>
    /// Gets the user who owns this refresh token.
    /// </summary>
    public User User { get; } = null!;

    /// <summary>
    /// Gets the refresh token that replaced this one.
    /// </summary>
    public RefreshToken? ReplacedBy { get; } = null!;

    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id,
        string tokenHash,
        DateTimeOffset expiresOnUtc,
        Guid userId,
        string deviceId,
        DateTimeOffset? revokedAt,
        RefreshTokenStatus status,
        Guid? replacedById
    )
        : base(id)
    {
        Guard.Against.NullOrWhiteSpace(tokenHash);
        Guard.Against.Default(expiresOnUtc);
        Guard.Against.Default(userId);
        Guard.Against.NullOrWhiteSpace(deviceId);

        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
        UserId = userId;
        DeviceId = deviceId;
        RevokedAt = revokedAt;
        Status = status;
        ReplacedById = replacedById;
    }

    /// <summary>
    /// Gets a value indicating whether the refresh token is expired.
    /// </summary>
    [Pure]
    public bool IsExpired(DateTimeOffset currentUtc) => ExpiresOnUtc <= currentUtc;

    /// <summary>
    /// Gets a value indicating whether the refresh token is valid (active and not expired).
    /// </summary>
    [Pure]
    public bool IsValid(DateTimeOffset currentUtc, string deviceId) =>
        Status == RefreshTokenStatus.Active &&
        !IsExpired(currentUtc) &&
        string.Equals(DeviceId, deviceId, StringComparison.Ordinal);

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="tokenHash">The unique token hash value.</param>
    /// <param name="expiresOnUtc">The expiration date and time in UTC.</param>
    /// <param name="userId">The ID of the user who owns this token.</param>
    /// <param name="deviceId">The device identifier associated with this token.</param>
    /// <returns>A new <see cref="RefreshToken"/> instance.</returns>
    [Pure]
    public static RefreshToken Create(string tokenHash, DateTimeOffset expiresOnUtc, Guid userId, string deviceId)
    {
        var created = new RefreshToken(
            id: Guid.CreateVersion7(),
            tokenHash: tokenHash,
            expiresOnUtc: expiresOnUtc,
            userId: userId,
            deviceId: deviceId,
            revokedAt: null,
            status: RefreshTokenStatus.Active,
            replacedById: null
        );

        created.AddDomainEvent(new RefreshTokenCreatedDomainEvent(RefreshTokenId: created.Id, UserId: created.UserId));

        return created;
    }

    public void MarkAsRevoked(DateTimeOffset revokedAt)
    {
        if (Status == RefreshTokenStatus.Revoked)
        {
            return;
        }

        Status = RefreshTokenStatus.Revoked;
        RevokedAt = revokedAt;
    }

    public void MarkAsCompromised(DateTimeOffset revokedAt)
    {
        if (Status == RefreshTokenStatus.Compromised)
        {
            return;
        }

        Status = RefreshTokenStatus.Compromised;
        RevokedAt = revokedAt;
    }

    public RefreshToken Rotate(string newTokenHash, DateTimeOffset newExpiresOnUtc, DateTimeOffset revokedAt)
    {
        var newRefreshToken = Create(
            tokenHash: newTokenHash,
            expiresOnUtc: newExpiresOnUtc,
            userId: UserId,
            deviceId: DeviceId);

        ReplacedById = newRefreshToken.Id;
        MarkAsRevoked(revokedAt);

        return newRefreshToken;
    }
}

public static class RefreshTokenExpressions
{
    public static Expression<Func<RefreshToken, bool>> IsExpired(Guid userId, DateTimeOffset currentUtc)
        => rt => rt.UserId == userId && rt.ExpiresOnUtc <= currentUtc;

    public static Expression<Func<RefreshToken, bool>> IsValid(Guid userId, DateTimeOffset currentUtc)
        => rt => rt.UserId == userId && rt.Status == RefreshTokenStatus.Active && currentUtc < rt.ExpiresOnUtc;
}