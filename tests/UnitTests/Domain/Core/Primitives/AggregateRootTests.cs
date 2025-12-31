using NetAuth.Domain.Core.Events;
using NetAuth.Domain.Core.Primitives;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace NetAuth.UnitTests.Domain.Core.Primitives;

public class AggregateRootTests
{
    #region Test Implementations

    private sealed record TestDomainEvent(Guid Id, string Message) : IDomainEvent;

    private sealed record AnotherDomainEvent(Guid Id) : IDomainEvent;

    private sealed class TestAggregate(Guid id, string name) : AggregateRoot<Guid>(id)
    {
        public string Name { get; } = name;

        public void DoSomething(string message) => AddDomainEvent(new TestDomainEvent(Id, message));

        public void DoAnotherThing() => AddDomainEvent(new AnotherDomainEvent(Id));
    }

    #endregion

    #region DomainEvents Tests

    [Fact]
    public void DomainEvents_WhenNoEventsAdded_ShouldBeEmpty()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act & Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_ShouldReturnReadOnlyList()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        var domainEvents = aggregate.DomainEvents;

        // Assert
        Assert.IsType<IReadOnlyList<IDomainEvent>>(domainEvents, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() =>
            ((IList<IDomainEvent>)domainEvents).Add(new TestDomainEvent(Guid.NewGuid(), "Test")));
    }

    [Fact]
    public void AddDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        aggregate.DoSomething("Hello");

        // Assert
        var domainEvent = Assert.Single(aggregate.DomainEvents);
        var testDomainEvent = Assert.IsType<TestDomainEvent>(domainEvent);
        Assert.Equal("Hello", testDomainEvent.Message);
        Assert.Equal(aggregate.Id, testDomainEvent.Id);
    }

    [Fact]
    public void AddDomainEvent_ShouldAddMultipleEvents()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        aggregate.DoSomething("First");
        aggregate.DoSomething("Second");
        aggregate.DoAnotherThing();

        // Assert
        Assert.Equal(3, aggregate.DomainEvents.Count);
        Assert.IsType<TestDomainEvent>(aggregate.DomainEvents[0]);
        Assert.IsType<TestDomainEvent>(aggregate.DomainEvents[1]);
        Assert.IsType<AnotherDomainEvent>(aggregate.DomainEvents[2]);
    }

    [Fact]
    public void DomainEvents_ShouldPreserveOrder()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        aggregate.DoSomething("First");
        aggregate.DoSomething("Second");
        aggregate.DoSomething("Third");

        // Assert
        var events = aggregate.DomainEvents.OfType<TestDomainEvent>().ToList();
        Assert.Equal("First", events[0].Message);
        Assert.Equal("Second", events[1].Message);
        Assert.Equal("Third", events[2].Message);
    }

    [Fact]
    public void DomainEvents_ShouldReturnSnapshot()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.DoSomething("Initial");

        // Act
        var snapshot = aggregate.DomainEvents;
        aggregate.DoSomething("After Snapshot");

        // Assert - Snapshot should not be affected by subsequent additions
        Assert.Single(snapshot);
        Assert.Equal(2, aggregate.DomainEvents.Count);
    }

    #endregion

    #region ClearDomainEvents Tests

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.DoSomething("First");
        aggregate.DoSomething("Second");
        Assert.Equal(2, aggregate.DomainEvents.Count);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_WhenEmpty_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        Assert.Empty(aggregate.DomainEvents);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => aggregate.ClearDomainEvents());
        Assert.Null(exception);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ShouldAllowAddingNewEvents()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.DoSomething("Before Clear");
        aggregate.ClearDomainEvents();

        // Act
        aggregate.DoSomething("After Clear");

        // Assert
        var domainEvent = Assert.Single(aggregate.DomainEvents);
        var testDomainEvent = Assert.IsType<TestDomainEvent>(domainEvent);
        Assert.Equal("After Clear", testDomainEvent.Message);
    }

    #endregion

    #region Inheritance from Entity Tests

    [Fact]
    public void AggregateRoot_ShouldInheritEntityBehavior()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregate(id, "Aggregate 1");
        var aggregate2 = new TestAggregate(id, "Aggregate 2");

        // Act & Assert - Should use Entity equality (by `Id`)
        Assert.True(aggregate1.Equals(aggregate2));
        Assert.Equal(aggregate1.GetHashCode(), aggregate2.GetHashCode());
    }

    [Fact]
    public void AggregateRoot_ShouldHaveAccessibleId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate(id, "Test");

        // Act & Assert
        Assert.Equal(id, aggregate.Id);
    }

    #endregion

    #region IAggregateRoot Interface Tests

    [Fact]
    public void IAggregateRoot_DomainEvents_ShouldBeAccessible()
    {
        // Arrange
        IAggregateRoot aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        ((TestAggregate)aggregate).DoSomething("Test");

        // Act & Assert
        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public void IAggregateRoot_ClearDomainEvents_ShouldWork()
    {
        // Arrange
        var testAggregate = new TestAggregate(Guid.NewGuid(), "Test");
        testAggregate.DoSomething("Test");
        IAggregateRoot aggregate = testAggregate;

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    #endregion
}