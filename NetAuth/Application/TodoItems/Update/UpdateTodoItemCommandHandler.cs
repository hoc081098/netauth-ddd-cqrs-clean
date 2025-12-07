using LanguageExt;
using static LanguageExt.Prelude;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;
using Unit = MediatR.Unit;

namespace NetAuth.Application.TodoItems.Update;

internal sealed class UpdateTodoItemCommandHandler(
    ITodoItemRepository todoItemRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IUserIdentifierProvider userIdentifierProvider
) : ICommandHandler<UpdateTodoItemCommand, Unit>
{
    public async Task<Either<DomainError, Unit>> Handle(UpdateTodoItemCommand command,
        CancellationToken cancellationToken)
    {
        var titleAndDescription = from todoTitle in TodoTitle.Create(command.Title)
            from todoDescription in TodoDescription.CreateOption(command.Description)
            select new { todoTitle, todoDescription };

        return await titleAndDescription
            .BindAsync(info =>
                CheckAndUpdate(
                    todoItemId: command.TodoItemId,
                    todoTitle: info.todoTitle,
                    description: info.todoDescription.MatchUnsafe(Some: identity, None: () => null),
                    dueDateOnUtc: command.DueDate.ToUniversalTime(),
                    labels: command.Labels,
                    cancellationToken: cancellationToken
                )
            );
    }

    private async Task<Either<DomainError, Unit>> CheckAndUpdate(
        Guid todoItemId,
        TodoTitle todoTitle,
        TodoDescription? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken)
    {
        var item = await todoItemRepository.GetByIdAsync(todoItemId, cancellationToken);
        if (item is null)
        {
            return TodoItemDomainErrors.TodoItem.NotFound;
        }

        if (item.UserId != userIdentifierProvider.UserId)
        {
            return TodoItemDomainErrors.TodoItem.NotOwnedByUser;
        }

        return await item
            .Update(
                title: todoTitle,
                description: description,
                dueDateOnUtc: dueDateOnUtc,
                labels: labels,
                currentUtc: clock.UtcNow
            )
            .MapAsync(_ =>
                unitOfWork.SaveChangesAsync(cancellationToken)
                    .Map(_ => Unit.Value)
            );
    }
}