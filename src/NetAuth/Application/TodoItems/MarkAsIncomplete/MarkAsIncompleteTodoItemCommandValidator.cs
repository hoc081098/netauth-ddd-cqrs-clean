using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.TodoItems.MarkAsIncomplete;

[UsedImplicitly]
internal sealed class MarkAsIncompleteTodoItemCommandValidator : AbstractValidator<MarkAsIncompleteTodoItemCommand>
{
    public MarkAsIncompleteTodoItemCommandValidator()
    {
        RuleFor(c => c.TodoItemId)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.MarkAsIncompleteTodoItem.IdIsRequired);
    }
}