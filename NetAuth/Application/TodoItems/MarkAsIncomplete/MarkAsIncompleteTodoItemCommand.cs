using MediatR;
using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.TodoItems.MarkAsIncomplete;

public sealed record MarkAsIncompleteTodoItemCommand(Guid TodoItemId) : ICommand<Unit>;
