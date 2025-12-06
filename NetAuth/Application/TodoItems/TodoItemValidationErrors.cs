using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Application.TodoItems;

public static class TodoItemValidationErrors
{
    public static class CreateTodoItem
    {
        public static readonly DomainError TitleIsRequired = new(
            code: "CreateTodoItem.TitleIsRequired",
            message: "Title is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TitleTooLong = new(
            code: "CreateTodoItem.TitleTooLong",
            message: $"Title cannot exceed {TodoTitle.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DescriptionTooLong = new(
            code: "CreateTodoItem.DescriptionTooLong",
            message: $"Description cannot exceed {TodoDescription.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError LabelsIsRequired = new(
            code: "CreateTodoItem.LabelsIsRequired",
            message: "Labels is required.",
            type: DomainError.ErrorType.Validation);
    }
}
