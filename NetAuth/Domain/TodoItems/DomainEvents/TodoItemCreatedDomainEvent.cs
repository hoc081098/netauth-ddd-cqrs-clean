using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.TodoItems.DomainEvents;

public sealed record TodoItemCreatedDomainEvent(Guid TodoItemId, Guid UserId) : IDomainEvent;