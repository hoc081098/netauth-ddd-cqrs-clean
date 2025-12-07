using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.Update;

[UsedImplicitly]
internal sealed class UpdateTodoItemCommandValidator: AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator(IClock clock)
    {
        RuleFor(c => c.Title)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.TitleIsRequired)
            .MaximumLength(TodoTitle.MaxLength)
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.TitleTooLong);

        RuleFor(c => c.Description)
            .MaximumLength(TodoDescription.MaxLength)
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.DescriptionTooLong);

        RuleFor(c => c.DueDate)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.DueDateIsRequired)
            .Must(dueDate => dueDate >= clock.UtcNow.StartOfDay())
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.DueDateMustBeTodayOrLater);

        RuleFor(c => c.Labels)
            .NotNull()
            .WithDomainError(TodoItemValidationErrors.UpdateTodoItem.LabelsIsRequired);
    }
}
