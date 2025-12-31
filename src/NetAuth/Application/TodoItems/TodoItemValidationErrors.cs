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

        public static readonly DomainError DueDateIsRequired = new(
            code: "CreateTodoItem.DueDateIsRequired",
            message: "Due date is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DueDateMustBeTodayOrLater = new(
            code: "CreateTodoItem.DueDateMustBeTodayOrLater",
            message: "Due date must be today or later.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError LabelsIsRequired = new(
            code: "CreateTodoItem.LabelsIsRequired",
            message: "Labels is required.",
            type: DomainError.ErrorType.Validation);
    }

    public static class UpdateTodoItem
    {
        public static readonly DomainError TitleIsRequired = new(
            code: "UpdateTodoItem.TitleIsRequired",
            message: "Title is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TitleTooLong = new(
            code: "UpdateTodoItem.TitleTooLong",
            message: $"Title cannot exceed {TodoTitle.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DescriptionTooLong = new(
            code: "UpdateTodoItem.DescriptionTooLong",
            message: $"Description cannot exceed {TodoDescription.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DueDateIsRequired = new(
            code: "UpdateTodoItem.DueDateIsRequired",
            message: "Due date is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DueDateMustBeTodayOrLater = new(
            code: "UpdateTodoItem.DueDateMustBeTodayOrLater",
            message: "Due date must be today or later.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError LabelsIsRequired = new(
            code: "UpdateTodoItem.LabelsIsRequired",
            message: "Labels is required.",
            type: DomainError.ErrorType.Validation);
    }

    public static class CompleteTodoItem
    {
        public static readonly DomainError IdIsRequired = new(
            code: "CompleteTodoItem.IdIsRequired",
            message: "Todo item ID is required.",
            type: DomainError.ErrorType.Validation);
    }

    public static class MarkAsIncompleteTodoItem
    {
        public static readonly DomainError IdIsRequired = new(
            code: "MarkAsIncompleteTodoItem.IdIsRequired",
            message: "Todo item ID is required.",
            type: DomainError.ErrorType.Validation);
    }
}