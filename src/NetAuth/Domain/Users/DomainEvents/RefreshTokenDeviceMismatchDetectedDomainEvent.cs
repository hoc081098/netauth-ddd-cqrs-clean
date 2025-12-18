using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

/// <summary>
/// Domain event raised when a device mismatch is detected for a refresh token.
/// This indicates potential token theft or unauthorized access attempt.
/// </summary>
public sealed record RefreshTokenDeviceMismatchDetectedDomainEvent(
    Guid RefreshTokenId,
    Guid UserId,
    string ExpectedDeviceId,
    string ActualDeviceId
) : IDomainEvent;

