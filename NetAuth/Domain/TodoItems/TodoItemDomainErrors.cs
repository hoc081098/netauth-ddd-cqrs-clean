using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.TodoItems;

public static class TodoItemDomainErrors
{
    public static class Title
    {
        public static readonly DomainError NullOrEmpty = new(
            code: "TodoItem.Title.NullOrEmpty",
            message: "Title is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TooLong = new(
            code: "TodoItem.Title.TooLong",
            message: $"Title cannot exceed {TodoTitle.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);
    }

    public static class Description
    {
        public static readonly DomainError TooLong = new(
            code: "TodoItem.Description.TooLong",
            message: $"Description cannot exceed {TodoDescription.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);
    }

    public static class TodoItem
    {
        public static readonly DomainError CannotUpdateCompletedItem = new(
            code: "TodoItem.CannotUpdateCompletedItem",
            message: "Cannot update a completed todo item.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError AlreadyCompleted = new(
            code: "TodoItem.AlreadyCompleted",
            message: "Todo item is already completed.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError NotCompleted = new(
            code: "TodoItem.NotCompleted",
            message: "Todo item is not completed.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError NotFound = new(
            code: "TodoItem.NotFound",
            message: "Todo item was not found.",
            type: DomainError.ErrorType.NotFound);

        public static readonly DomainError NotOwnedByUser = new(
            code: "TodoItem.NotOwnedByUser",
            message: "Todo item does not belong to the current user.",
            type: DomainError.ErrorType.Forbidden);
    }
}