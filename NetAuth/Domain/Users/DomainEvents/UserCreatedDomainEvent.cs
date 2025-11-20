using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

public sealed class UserCreatedDomainEvent : IDomainEvent
{
    internal UserCreatedDomainEvent(User user) => User = user;

    public User User { get; }
}