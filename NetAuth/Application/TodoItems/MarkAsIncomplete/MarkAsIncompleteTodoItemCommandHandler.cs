using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;
using Unit = MediatR.Unit;

namespace NetAuth.Application.TodoItems.MarkAsIncomplete;

internal sealed class MarkAsIncompleteTodoItemCommandHandler(
    ITodoItemRepository todoItemRepository,
    IUnitOfWork unitOfWork,
    IUserIdentifierProvider userIdentifierProvider
) : ICommandHandler<MarkAsIncompleteTodoItemCommand, Unit>
{
    public async Task<Either<DomainError, Unit>> Handle(MarkAsIncompleteTodoItemCommand command,
        CancellationToken cancellationToken)
    {
        var item = await todoItemRepository.GetByIdAsync(command.TodoItemId, cancellationToken);
        if (item is null)
        {
            return TodoItemDomainErrors.TodoItem.NotFound;
        }

        if (item.UserId != userIdentifierProvider.UserId)
        {
            return TodoItemDomainErrors.TodoItem.NotOwnedByUser;
        }

        var result = item.MarkAsIncomplete();

        return await result.MapAsync(async _ =>
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        });
    }
}
