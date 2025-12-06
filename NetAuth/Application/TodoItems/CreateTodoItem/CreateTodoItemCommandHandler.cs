using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.CreateTodoItem;

internal sealed class CreateTodoItemCommandHandler(
    ITodoItemRepository todoItemRepository,
    IUserIdentifierProvider userIdentifierProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateTodoItemCommand, CreateTodoItemResult>
{
    public async Task<Either<DomainError, CreateTodoItemResult>> Handle(
        CreateTodoItemCommand command,
        CancellationToken cancellationToken
    ) =>
        await TodoItem
            .Create(
                userId: userIdentifierProvider.UserId,
                title: command.Title,
                description: command.Description,
                dueDateOnUtc: command.DueDate.ToUniversalTime(),
                labels: command.Labels
            )
            .MapAsync(async todoItem =>
            {
                todoItemRepository.Insert(todoItem);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new CreateTodoItemResult(todoItem.Id);
            });
}