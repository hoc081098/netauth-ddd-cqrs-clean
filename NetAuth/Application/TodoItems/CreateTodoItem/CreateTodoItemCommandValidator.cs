using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems.CreateTodoItem;

[UsedImplicitly]
internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleIsRequired)
            .MaximumLength(TodoTitle.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.TitleTooLong);

        RuleFor(x => x.Description)
            .MaximumLength(TodoDescription.MaxLength)
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.DescriptionTooLong);

        RuleFor(x => x.Labels)
            .NotNull()
            .WithDomainError(TodoItemValidationErrors.CreateTodoItem.LabelsIsRequired);
    }
}