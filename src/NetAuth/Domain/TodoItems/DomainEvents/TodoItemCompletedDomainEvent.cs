using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.TodoItems.DomainEvents;

public sealed record TodoItemCompletedDomainEvent(Guid TodoItemId, Guid UserId) : IDomainEvent;