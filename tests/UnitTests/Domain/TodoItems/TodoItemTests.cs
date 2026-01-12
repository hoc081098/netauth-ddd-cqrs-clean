using LanguageExt.UnitTesting;
using NetAuth.Domain.TodoItems;
using NetAuth.Domain.TodoItems.DomainEvents;
using NetAuth.TestUtils;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.TodoItems;

public class TodoItemTests : BaseTest
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.UserId;
        var title = TodoItemTestData.Title.Value;
        var description = TodoItemTestData.Description.Value;
        var dueDateOnUtc = TodoItemTestData.FutureDueDate;
        var labels = TodoItemTestData.NonEmptyLabels;
        var currentUtc = TodoItemTestData.CurrentUtc;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

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
        var userId = TodoItemTestData.UserId;
        var title = TodoItemTestData.Title.Value;
        string? description = null;
        var dueDateOnUtc = TodoItemTestData.FutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.CurrentUtc;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

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
        var userId = TodoItemTestData.UserId;
        var title = ""; // Invalid
        var description = TodoItemTestData.Description.Value;
        var dueDateOnUtc = TodoItemTestData.FutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.CurrentUtc;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Title.NullOrEmpty, left));
    }

    [Fact]
    public void Create_WithPastDueDate_ShouldReturnError()
    {
        // Arrange
        var userId = TodoItemTestData.UserId;
        var title = TodoItemTestData.Title.Value;
        var description = TodoItemTestData.Description.Value;
        var currentUtc = TodoItemTestData.CurrentUtc;
        var dueDateOnUtc = currentUtc.AddDays(-1); // Past date
        var labels = TodoItemTestData.EmptyLabels;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));
    }

    [Fact]
    public void Create_WithCurrentDueDate_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.UserId;
        var title = TodoItemTestData.Title.Value;
        var description = TodoItemTestData.Description.Value;
        var currentUtc = TodoItemTestData.CurrentUtc;
        var dueDateOnUtc = currentUtc; // Current date (valid)
        var labels = TodoItemTestData.EmptyLabels;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeRight(todoItem => Assert.Equal(dueDateOnUtc, todoItem.DueDateOnUtc));
    }

    [Fact]
    public void Update_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var currentUtc = TodoItemTestData.CurrentUtc;
        var todoItem = TodoItemTestData.CreateTodoItem();

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var newDueDateOnUtc = currentUtc.AddDays(3);
        List<string> newLabels = ["updated", "label"];

        // Act
        var result = todoItem.Update(title: newTitle,
            description: newDescription,
            dueDateOnUtc: newDueDateOnUtc,
            labels: newLabels,
            currentUtc: currentUtc);

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
        var currentUtc = TodoItemTestData.CurrentUtc;
        var todoItem = TodoItemTestData.CreateCompletedTodoItem();

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var newDueDateOnUtc = currentUtc.AddDays(1);
        var newLabels = TodoItemTestData.EmptyLabels;

        // Act
        var result = todoItem.Update(title: newTitle,
            description: newDescription,
            dueDateOnUtc: newDueDateOnUtc,
            labels: newLabels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem, left));
    }

    [Fact]
    public void Update_WithPastDueDate_ShouldReturnError()
    {
        // Arrange
        var currentUtc = TodoItemTestData.CurrentUtc;
        var todoItem = TodoItemTestData.CreateTodoItem();

        var newTitle = TodoTitle.Create("Updated title").RightValueOrThrow();
        var newDescription = TodoDescription.Create("Updated description").RightValueOrThrow();
        var pastDueDate = currentUtc.AddDays(-1); // Past date
        var newLabels = TodoItemTestData.EmptyLabels;

        // Act
        var result = todoItem.Update(title: newTitle,
            description: newDescription,
            dueDateOnUtc: pastDueDate,
            labels: newLabels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));
    }

    [Fact]
    public void MarkAsCompleted_WithIncompleteTodoItem_ShouldReturnSuccess()
    {
        // Arrange
        var currentUtc = TodoItemTestData.CurrentUtc;
        var todoItem = TodoItemTestData.CreateTodoItem();
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
        var currentUtc = TodoItemTestData.CurrentUtc;
        var todoItem = TodoItemTestData.CreateCompletedTodoItem();

        // Act
        var result = todoItem.MarkAsCompleted(currentUtc.AddHours(1));

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.AlreadyCompleted, left));
    }

    [Fact]
    public void MarkAsIncomplete_WithCompletedTodoItem_ShouldReturnSuccess()
    {
        // Arrange
        var todoItem = TodoItemTestData.CreateCompletedTodoItem();

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
        var todoItem = TodoItemTestData.CreateTodoItem();

        // Act
        var result = todoItem.MarkAsIncomplete();

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.TodoItem.NotCompleted, left));
    }

    [Fact]
    public void Create_WithEmptyLabels_ShouldReturnSuccess()
    {
        // Arrange
        var userId = TodoItemTestData.UserId;
        var title = TodoItemTestData.Title.Value;
        var description = TodoItemTestData.Description.Value;
        var dueDateOnUtc = TodoItemTestData.FutureDueDate;
        var labels = TodoItemTestData.EmptyLabels;
        var currentUtc = TodoItemTestData.CurrentUtc;

        // Act
        var result = TodoItem.Create(userId: userId,
            title: title,
            description: description,
            dueDateOnUtc: dueDateOnUtc,
            labels: labels,
            currentUtc: currentUtc);

        // Assert
        result.ShouldBeRight(todoItem => Assert.Empty(todoItem.Labels));
    }

    [Fact]
    public void Labels_ShouldBeReadOnly()
    {
        // Arrange
        var todoItem = TodoItemTestData.CreateTodoItem();

        // Act & Assert
        Assert.IsType<IReadOnlyList<string>>(todoItem.Labels, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() =>
            ((IList<string>)todoItem.Labels).Add("new-label"));
    }
}