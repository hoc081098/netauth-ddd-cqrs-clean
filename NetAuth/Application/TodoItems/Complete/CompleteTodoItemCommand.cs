using MediatR;
using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.TodoItems.Complete;

public sealed record CompleteTodoItemCommand(Guid TodoItemId) : ICommand<Unit>;