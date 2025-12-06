using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.TodoItems.GetTodoItems;

public sealed record GetTodoItemsQuery : IQuery<GetTodoItemsResult>;

public sealed record GetTodoItemsResult(IReadOnlyList<TodoItemResponse> TodoItems);

public sealed record TodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset? CompletedOnUtc,
    DateTimeOffset DueDateOnUtc,
    IReadOnlyList<string> Labels,
    DateTimeOffset CreatedOnUtc
);