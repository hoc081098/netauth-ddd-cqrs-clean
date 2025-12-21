using NetAuth.Domain.Core.Events;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.UnitTests.Domain;

public abstract class BaseTest
{
    protected static T AssertDomainEventWasPublished<T>(IAggregateRoot aggregateRoot)
        where T : IDomainEvent
    {
        var domainEvent = aggregateRoot.DomainEvents.OfType<T>().SingleOrDefault();
        return domainEvent ?? throw new Exception($"The domain event of type {typeof(T).Name} was not published.");
    }
}