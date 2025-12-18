using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

/// <summary>
/// Domain event raised when an expired refresh token is used.
/// </summary>
public sealed record RefreshTokenExpiredUsageDomainEvent(
    Guid RefreshTokenId,
    Guid UserId,
    DateTimeOffset ExpiresOnUtc,
    DateTimeOffset AttemptedAt
) : IDomainEvent;

