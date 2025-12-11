using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.Get;

internal sealed class GetTodoItemsQueryHandler(
    ITodoItemRepository todoItemRepository,
    IUserIdentifierProvider userIdentifierProvider
) : IQueryHandler<GetTodoItemsQuery, GetTodoItemsResult>
{
    public async Task<Either<DomainError, GetTodoItemsResult>> Handle(
        GetTodoItemsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = userIdentifierProvider.UserId;

        var todoItems = (await todoItemRepository.GetTodoItemsByUserId(userId, cancellationToken))
            .Select(ToTodoItemResponse)
            .ToArray();

        return new GetTodoItemsResult(todoItems);
    }

    private static TodoItemResponse ToTodoItemResponse(TodoItem todoItem) =>
        new(
            Id: todoItem.Id,
            Title: todoItem.Title,
            Description: todoItem.Description ?? string.Empty,
            IsCompleted: todoItem.IsCompleted,
            CompletedOnUtc: todoItem.CompletedOnUtc,
            DueDateOnUtc: todoItem.DueDateOnUtc,
            Labels: todoItem.Labels,
            CreatedOnUtc: todoItem.CreatedOnUtc
        );
}