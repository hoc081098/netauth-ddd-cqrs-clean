using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;