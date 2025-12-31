// filepath: /Users/hoc.nguyen/Desktop/My/NetAuth/tests/UnitTests/Domain/TodoItems/TodoItemTestData.cs

using LanguageExt.UnitTesting;
using NetAuth.Domain.TodoItems;

namespace NetAuth.UnitTests.Domain.TodoItems;

/// <summary>
/// Shared test data for TodoItem-related tests.
/// </summary>
public static class TodoItemTestData
{
    #region User Data

    public static readonly Guid UserId = Guid.NewGuid();

    #endregion

    #region TodoItem Data

    public static readonly TodoTitle Title = TodoTitle
        .Create("Buy groceries")
        .RightValueOrThrow();

    public static readonly TodoDescription Description = TodoDescription
        .Create("Get milk, eggs, and bread")
        .RightValueOrThrow();

    public static readonly IReadOnlyList<string> NonEmptyLabels = ["shopping", "urgent"];

    public static readonly IReadOnlyList<string> EmptyLabels = [];

    #endregion

    #region Common Timestamps

    /// <summary>
    /// Fixed point in time for consistent test results across all TodoItem tests.
    /// </summary>
    public static readonly DateTimeOffset CurrentUtc =
        new(year: 2025,
            month: 1,
            day: 1,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    public static readonly DateTimeOffset FutureDueDate = CurrentUtc.AddDays(1);

    public static readonly DateTimeOffset PastDueDate = CurrentUtc.AddDays(-1);

    #endregion

    #region Factory Methods

    public static TodoItem CreateTodoItem(
        Guid? userId = null,
        string? title = null,
        string? description = null,
        DateTimeOffset? dueDateOnUtc = null,
        IReadOnlyList<string>? labels = null,
        DateTimeOffset? currentUtc = null) =>
        TodoItem
            .Create(
                userId: userId ?? UserId,
                title: title ?? Title.Value,
                description: description ?? Description.Value,
                dueDateOnUtc: dueDateOnUtc ?? FutureDueDate,
                labels: labels ?? NonEmptyLabels,
                currentUtc: currentUtc ?? CurrentUtc
            )
            .RightValueOrThrow();

    public static TodoItem CreateCompletedTodoItem(
        Guid? userId = null,
        DateTimeOffset? completedOnUtc = null)
    {
        var todoItem = CreateTodoItem(userId: userId);
        todoItem.MarkAsCompleted(completedOnUtc ?? CurrentUtc).ShouldBeRight();
        return todoItem;
    }

    #endregion
}