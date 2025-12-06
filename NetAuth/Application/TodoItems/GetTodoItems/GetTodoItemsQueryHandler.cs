using LanguageExt;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.GetTodoItems;

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

        var todoItems = await todoItemRepository.GetTodoItemsByUserId(userId)
            .Select(t =>
                new TodoItemResponse(
                    Id: t.Id,
                    Title: t.Title,
                    Description: t.Description ?? string.Empty,
                    IsCompleted: t.IsCompleted,
                    CompletedOnUtc: t.CompletedOnUtc,
                    DueDateOnUtc: t.DueDateOnUtc,
                    Labels: t.Labels,
                    CreatedOnUtc: t.CreatedOnUtc
                )
            )
            .ToListAsync(cancellationToken);

        return new GetTodoItemsResult(todoItems);
    }
}