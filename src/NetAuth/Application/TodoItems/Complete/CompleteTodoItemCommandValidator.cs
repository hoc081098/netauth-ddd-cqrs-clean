using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.TodoItems.Complete;

[UsedImplicitly]
internal sealed class CompleteTodoItemCommandValidator : AbstractValidator<CompleteTodoItemCommand>
{
    public CompleteTodoItemCommandValidator()
    {
        RuleFor(c => c.TodoItemId)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CompleteTodoItem.IdIsRequired);
    }
}