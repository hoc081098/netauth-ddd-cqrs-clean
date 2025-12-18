using System.Linq.Expressions;
using Ardalis.GuardClauses;
using JetBrains.Annotations;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Domain.Users;

public enum RefreshTokenStatus
{
    Active = 0,
    Revoked = 1,
    Compromised = 2 // Consider entire token chain is compromised
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
    public RefreshToken? ReplacedBy { get; }

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
    public bool IsValid(DateTimeOffset currentUtc) =>
        Status == RefreshTokenStatus.Active &&
        !IsExpired(currentUtc);

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

    /// <summary>
    /// Marks the refresh token as revoked due to expiration.
    /// Raises a domain event for security audit logging.
    /// </summary>
    /// <param name="revokedAt">The date and time when the expired token was used.</param>
    public void MarkAsRevokedDueToExpiration(DateTimeOffset revokedAt)
    {
        MarkAsRevokedInternal(revokedAt);

        AddDomainEvent(new RefreshTokenExpiredUsageDomainEvent(
            RefreshTokenId: Id,
            UserId: UserId,
            ExpiresOnUtc: ExpiresOnUtc,
            AttemptedAt: revokedAt));
    }

    /// <summary>
    /// Marks the refresh token as compromised due to device mismatch.
    /// Raises domain events for security audit logging.
    /// </summary>
    /// <param name="revokedAt">The date and time when the device mismatch was detected.</param>
    /// <param name="actualDeviceId">The device ID that was provided in the request.</param>
    public void MarkAsCompromisedDueToDeviceMismatch(DateTimeOffset revokedAt, string actualDeviceId)
    {
        MarkAsCompromisedInternal(revokedAt, raiseChainEvent: false);

        AddDomainEvent(new RefreshTokenDeviceMismatchDetectedDomainEvent(
            RefreshTokenId: Id,
            UserId: UserId,
            ExpectedDeviceId: DeviceId,
            ActualDeviceId: actualDeviceId));
    }

    /// <summary>
    /// Marks the refresh token as compromised due to token reuse detection.
    /// Raises domain events for security audit logging.
    /// </summary>
    /// <param name="detectedAt">The date and time when the reuse was detected.</param>
    /// <param name="chainAffected">Indicates whether the entire token chain should be marked as compromised.</param>
    public void MarkAsCompromisedDueToReuse(DateTimeOffset detectedAt, bool chainAffected) =>
        MarkAsCompromisedInternal(detectedAt, raiseChainEvent: chainAffected);

    /// <summary>
    /// Rotates the refresh token by creating a new one and revoking the current one.
    /// Raises a domain event for security audit logging.
    /// </summary>
    /// <param name="newTokenHash">The hash of the new token.</param>
    /// <param name="newExpiresOnUtc">The expiration date of the new token.</param>
    /// <param name="revokedAt">The date and time when the current token is revoked.</param>
    /// <returns>The new <see cref="RefreshToken"/> instance.</returns>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset newExpiresOnUtc, DateTimeOffset revokedAt)
    {
        var newRefreshToken = Create(
            tokenHash: newTokenHash,
            expiresOnUtc: newExpiresOnUtc,
            userId: UserId,
            deviceId: DeviceId);

        ReplacedById = newRefreshToken.Id;
        MarkAsRevokedInternal(revokedAt);

        AddDomainEvent(new RefreshTokenRotatedDomainEvent(
            OldRefreshTokenId: Id,
            NewRefreshTokenId: newRefreshToken.Id,
            UserId: UserId,
            DeviceId: DeviceId));

        return newRefreshToken;
    }

    /// <summary>
    /// Marks the refresh token as revoked.
    /// </summary>
    /// <param name="revokedAt">The date and time when the token was revoked.</param>
    private void MarkAsRevokedInternal(DateTimeOffset revokedAt)
    {
        if (Status == RefreshTokenStatus.Revoked)
        {
            return;
        }

        Status = RefreshTokenStatus.Revoked;
        RevokedAt = revokedAt;
    }

    /// <summary>
    /// Marks the refresh token as compromised (potential security breach).
    /// Raises a domain event for security audit logging.
    /// </summary>
    /// <param name="revokedAt">The date and time when the compromise was detected.</param>
    /// <param name="raiseChainEvent">Indicates whether to raise a chain compromised event.</param>
    private void MarkAsCompromisedInternal(DateTimeOffset revokedAt, bool raiseChainEvent)
    {
        if (Status == RefreshTokenStatus.Compromised)
        {
            return;
        }

        var previousStatus = Status;
        Status = RefreshTokenStatus.Compromised;
        RevokedAt = revokedAt;

        AddDomainEvent(new RefreshTokenReuseDetectedDomainEvent(
            RefreshTokenId: Id,
            UserId: UserId,
            DeviceId: DeviceId,
            PreviousStatus: previousStatus));

        if (raiseChainEvent)
        {
            AddDomainEvent(new RefreshTokenChainCompromisedDomainEvent(UserId: UserId));
        }
    }
}

public static class RefreshTokenExpressions
{
    public static Expression<Func<RefreshToken, bool>> IsExpired(Guid userId, DateTimeOffset currentUtc)
        => rt => rt.UserId == userId && rt.ExpiresOnUtc <= currentUtc;

    public static Expression<Func<RefreshToken, bool>> IsValid(Guid userId, DateTimeOffset currentUtc)
        => rt => rt.UserId == userId && rt.Status == RefreshTokenStatus.Active && currentUtc < rt.ExpiresOnUtc;
}