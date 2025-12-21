using LanguageExt.UnitTesting;
using NetAuth.Domain.TodoItems;
using NetAuth.Domain.TodoItems.DomainEvents;

namespace NetAuth.UnitTests.Domain.TodoItems;

public static class TodoItemTestData
{
    public static readonly Guid ValidUserId = Guid.NewGuid();
    public static readonly TodoTitle ValidTitle = TodoTitle.Create("Buy groceries").RightValueOrThrow();
    public static readonly TodoDescription ValidDescription = TodoDescription.Create("Get milk, eggs, and bread").RightValueOrThrow();
    public static readonly IReadOnlyList<string> ValidLabels = new List<string> { "shopping", "urgent" };
    public static readonly IReadOnlyList<string> EmptyLabels = new List<string>();
    public static readonly DateTimeOffset ValidCurrentUtc = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset ValidFutureDueDate = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);
}

public class TodoItemTests : BaseTest
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = "Buy groceries";
        var description = "Get milk and bread";
        var dueDateOnUtc = TodoItemTestData.ValidFutureDueDate;
        var labels = TodoItemTestData.ValidLabels;
        var currentUtc = TodoItemTestData.ValidCurrentUtc;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeRight(todoItem =>
        {
            Assert.NotEqual(Guid.Empty, todoItem.Id);
            Assert.Equal(userId, todoItem.UserId);
            Assert.Equal(title, todoItem.Title.Value);
            Assert.NotNull(todoItem.Description);
            Assert.Equal(description, todoItem.Description!.Value);
            Assert.Equal(dueDateOnUtc, todoItem.DueDateOnUtc);
            Assert.Equal(labels, todoItem.Labels);
            Assert.False(todoItem.IsCompleted);
            Assert.Null(todoItem.CompletedOnUtc);

            var domainEvent = AssertDomainEventWasPublished<TodoItemCreatedDomainEvent>(todoItem);
            Assert.Equal(todoItem.Id, domainEvent.TodoItemId);
            Assert.Equal(userId, domainEvent.UserId);
        });
    }

    [Fact]
    public void Create_WithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = "Simple task";
        string? description = null;
        var dueDateOnUtc = TodoItemTestData.ValidFutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.ValidCurrentUtc;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeRight(todoItem =>
        {
            Assert.NotNull(todoItem);
            Assert.Null(todoItem.Description);
        });
    }

    [Fact]
    public void Create_WithInvalidTitle_ShouldReturnError()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = ""; // Invalid
        var description = "Valid description";
        var dueDateOnUtc = TodoItemTestData.ValidFutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.ValidCurrentUtc;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Title.NullOrEmpty, left));
    }

    [Fact]
    public void Create_WithPastDueDate_ShouldReturnError()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = "Task";
        var description = "Description";
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var dueDateOnUtc = currentUtc.AddDays(-1); // Past date
        var labels = TodoItemTestData.EmptyLabels;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));
    }

    [Fact]
    public void Create_WithCurrentDueDate_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = "Task";
        var description = "Description";
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var dueDateOnUtc = currentUtc; // Current date (valid)
        var labels = TodoItemTestData.EmptyLabels;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeRight(todoItem =>
        {
            Assert.Equal(dueDateOnUtc, todoItem.DueDateOnUtc);
        });
    }

    [Fact]
    public void Update_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var newDueDateOnUtc = currentUtc.AddDays(3);
        var newLabels = new List<string> { "updated", "label" }.AsReadOnly();

        // Act
        var result = todoItem.Update(newTitle, newDescription, newDueDateOnUtc, newLabels, currentUtc);

        // Assert
        result.ShouldBeRight(_ =>
        {
            Assert.Equal(newTitle, todoItem.Title);
            Assert.Equal(newDescription, todoItem.Description);
            Assert.Equal(newDueDateOnUtc, todoItem.DueDateOnUtc);
            Assert.Equal(newLabels, todoItem.Labels);
        });
    }

    [Fact]
    public void Update_CompletedTodoItem_ShouldReturnError()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);
        todoItem.MarkAsCompleted(currentUtc);

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var newDueDateOnUtc = currentUtc.AddDays(1);
        var newLabels = TodoItemTestData.EmptyLabels;

        // Act
        var result = todoItem.Update(newTitle, newDescription, newDueDateOnUtc, newLabels, currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem, left));
    }

    [Fact]
    public void Update_WithPastDueDate_ShouldReturnError()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var pastDueDate = currentUtc.AddDays(-1); // Past date
        var newLabels = TodoItemTestData.EmptyLabels;

        // Act
        var result = todoItem.Update(newTitle, newDescription, pastDueDate, newLabels, currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));
    }

    [Fact]
    public void MarkAsCompleted_WithIncompleteTodoItem_ShouldReturnSuccess()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);
        var completedOnUtc = currentUtc.AddHours(1);

        // Act
        var result = todoItem.MarkAsCompleted(completedOnUtc);

        // Assert
        result.ShouldBeRight(_ =>
        {
            Assert.True(todoItem.IsCompleted);
            Assert.Equal(completedOnUtc, todoItem.CompletedOnUtc);

            var domainEvent = AssertDomainEventWasPublished<TodoItemCompletedDomainEvent>(todoItem);
            Assert.Equal(todoItem.Id, domainEvent.TodoItemId);
            Assert.Equal(todoItem.UserId, domainEvent.UserId);
        });
    }

    [Fact]
    public void MarkAsCompleted_WithAlreadyCompletedTodoItem_ShouldReturnError()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);
        todoItem.MarkAsCompleted(currentUtc);

        // Act
        var result = todoItem.MarkAsCompleted(currentUtc.AddHours(1));

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.AlreadyCompleted, left));
    }

    [Fact]
    public void MarkAsIncomplete_WithCompletedTodoItem_ShouldReturnSuccess()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);
        todoItem.MarkAsCompleted(currentUtc);

        // Act
        var result = todoItem.MarkAsIncomplete();

        // Assert
        result.ShouldBeRight(_ =>
        {
            Assert.False(todoItem.IsCompleted);
            Assert.Null(todoItem.CompletedOnUtc);
        });
    }

    [Fact]
    public void MarkAsIncomplete_WithIncompleteTodoItem_ShouldReturnError()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);

        // Act
        var result = todoItem.MarkAsIncomplete();

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.NotCompleted, left));
    }

    [Fact]
    public void Create_WithEmptyLabels_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.ValidUserId;
        var title = "Task without labels";
        var description = "Description";
        var dueDateOnUtc = TodoItemTestData.ValidFutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.ValidCurrentUtc;

        // Act
        var result = TodoItem.Create(userId, title, description, dueDateOnUtc, labels, currentUtc);

        // Assert
        result.ShouldBeRight(todoItem =>
        {
            Assert.Empty(todoItem.Labels);
        });
    }

    [Fact]
    public void Labels_ShouldBeReadOnly()
    {
        // Arrange
        var currentUtc = TodoItemTestData.ValidCurrentUtc;
        var todoItem = CreateValidTodoItem(currentUtc);

        // Act & Assert
        Assert.IsType<IReadOnlyList<string>>(todoItem.Labels, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() => ((IList<string>)todoItem.Labels).Add("new-label"));
    }

    private static TodoItem CreateValidTodoItem(DateTimeOffset currentUtc)
    {
        return TodoItem.Create(
            userId: TodoItemTestData.ValidUserId,
            title: "Valid title",
            description: "Valid description",
            dueDateOnUtc: currentUtc.AddDays(1),
            labels: TodoItemTestData.ValidLabels,
            currentUtc: currentUtc
        ).RightValueOrThrow();
    }
}

