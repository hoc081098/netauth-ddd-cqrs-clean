using MediatR;
using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.TodoItems.Update;

public sealed record UpdateTodoItemCommand(
    Guid TodoItemId,
    string Title,
    string? Description,
    DateTimeOffset DueDate,
    IReadOnlyList<string> Labels
) : ICommand<Unit>;