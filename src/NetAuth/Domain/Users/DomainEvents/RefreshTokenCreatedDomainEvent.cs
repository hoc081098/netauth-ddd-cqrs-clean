using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

public sealed record RefreshTokenCreatedDomainEvent(
    Guid RefreshTokenId,
    Guid UserId
) : IDomainEvent;