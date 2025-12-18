using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.Create;

[UsedImplicitly]
internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator(IClock clock)
    {
        RuleFor(c => c.Title)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleIsRequired)
            .MaximumLength(TodoTitle.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleTooLong);

        RuleFor(c => c.Description)
            .MaximumLength(TodoDescription.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DescriptionTooLong);

        RuleFor(c => c.DueDate)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DueDateIsRequired)
            .Must(dueDate => dueDate >= clock.UtcNow.StartOfDay())
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DueDateMustBeTodayOrLater);

        RuleFor(c => c.Labels)
            .NotNull()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.LabelsIsRequired);
    }
}