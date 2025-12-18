using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

/// <summary>
/// Domain event raised when a refresh token is successfully rotated.
/// </summary>
public sealed record RefreshTokenRotatedDomainEvent(
    Guid OldRefreshTokenId,
    Guid NewRefreshTokenId,
    Guid UserId,
    string DeviceId
) : IDomainEvent;

