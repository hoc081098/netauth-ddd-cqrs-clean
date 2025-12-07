using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.TodoItems.Create;

public sealed record CreateTodoItemCommand(
    string Title,
    string? Description,
    DateTimeOffset DueDate,
    IReadOnlyList<string> Labels
) : ICommand<CreateTodoItemResult>;

public sealed record CreateTodoItemResult(Guid TodoItemId);