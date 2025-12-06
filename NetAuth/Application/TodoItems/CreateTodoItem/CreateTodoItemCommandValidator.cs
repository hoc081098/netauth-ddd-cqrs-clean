using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.CreateTodoItem;

[UsedImplicitly]
internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator(IClock clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleIsRequired)
            .MaximumLength(TodoTitle.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleTooLong);

        RuleFor(x => x.Description)
            .MaximumLength(TodoDescription.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DescriptionTooLong);

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DueDateIsRequired)
            .Must(dueDate => dueDate >= clock.UtcNow.StartOfDay())
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DueDateMustBeTodayOrLater);

        RuleFor(x => x.Labels)
            .NotNull()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.LabelsIsRequired);
    }
}