using System.Diagnostics.CodeAnalysis;
using NetAuth.Domain.Core.Events;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.UnitTests.Domain;

public abstract class BaseTest
{
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
    protected static T AssertDomainEventWasPublished<T>(IAggregateRoot aggregateRoot)
        where T : IDomainEvent
    {
        var domainEvent = aggregateRoot.DomainEvents.OfType<T>().SingleOrDefault();
        return domainEvent ?? throw new Exception($"The domain event of type {typeof(T).Name} was not published.");
    }
}