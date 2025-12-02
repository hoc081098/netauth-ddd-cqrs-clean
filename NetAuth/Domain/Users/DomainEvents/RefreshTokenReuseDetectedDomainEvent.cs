using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

/// <summary>
/// Domain event raised when a refresh token reuse is detected (security breach).
/// This indicates potential token theft.
/// </summary>
public sealed record RefreshTokenReuseDetectedDomainEvent(
    Guid RefreshTokenId,
    Guid UserId,
    string DeviceId,
    RefreshTokenStatus PreviousStatus
) : IDomainEvent;

