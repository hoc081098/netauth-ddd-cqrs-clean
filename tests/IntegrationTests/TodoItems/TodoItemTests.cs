using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.TodoItems.Complete;
using NetAuth.Application.TodoItems.Create;
using NetAuth.Application.TodoItems.Get;
using NetAuth.Application.TodoItems.MarkAsIncomplete;
using NetAuth.Application.TodoItems.Update;
using NetAuth.Application.Users.Register;
using NetAuth.Domain.TodoItems;
using NetAuth.Domain.TodoItems.DomainEvents;
using NetAuth.IntegrationTests.Infrastructure;
using NetAuth.TestUtils;
using Xunit.Abstractions;

namespace NetAuth.IntegrationTests.TodoItems;

[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
public class TodoItemTests(IntegrationTestWebAppFactory webAppFactory, ITestOutputHelper testOutputHelper)
    : BaseIntegrationTest(webAppFactory, testOutputHelper)
{
    public override void Dispose()
    {
        TestUserContext.ClearCurrentUser();
        base.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test user and sets them as the current authenticated user.
    /// </summary>
    private async Task<Guid> CreateAndAuthenticateUserAsync(string username, string email)
    {
        var registerResult = await Sender.Send(new RegisterCommand(
            Username: username,
            Email: email,
            Password: "Password123!"));

        var userId = registerResult.RightValueOrThrow().UserId;
        TestUserContext.SetCurrentUser(userId);
        return userId;
    }

    /// <summary>
    /// Creates a todo item for the current authenticated user.
    /// </summary>
    private async Task<Guid> CreateTodoItemAsync(
        string title = "Test Todo",
        string? description = "Test Description",
        int daysFromNow = 7,
        IReadOnlyList<string>? labels = null)
    {
        var command = new CreateTodoItemCommand(
            Title: title,
            Description: description,
            DueDate: DateTimeOffset.UtcNow.AddDays(daysFromNow),
            Labels: labels ?? ["test"]);

        var result = await Sender.Send(command);
        return result.RightValueOrThrow().TodoItemId;
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldCreateTodoItemAndStoreOutboxMessage()
    {
        // Arrange
        var userId = await CreateAndAuthenticateUserAsync(
            username: "create_todo_user",
            email: "create_todo@example.com");

        var command = new CreateTodoItemCommand(
            Title: "My First Todo",
            Description: "This is a test todo item",
            DueDate: DateTimeOffset.UtcNow.AddDays(7),
            Labels: ["work", "urgent"]);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.ShouldBeRight(r => Assert.NotEqual(Guid.Empty, r.TodoItemId));

        var todoItemId = result.RightValueOrThrow().TodoItemId;

        // Verify todo item was created in database
        var createdItem = await DbContext.TodoItems
            .SingleAsync(t => t.Id == todoItemId);

        Assert.Equal("My First Todo", createdItem.Title.Value);
        Assert.Equal("This is a test todo item", createdItem.Description?.Value);
        Assert.False(createdItem.IsCompleted);
        Assert.Null(createdItem.CompletedOnUtc);
        Assert.Equal(userId, createdItem.UserId);
        Assert.Equal(2, createdItem.Labels.Count);
        Assert.Contains("work", createdItem.Labels);
        Assert.Contains("urgent", createdItem.Labels);

        // Verify outbox message was created for TodoItemCreatedDomainEvent
        var todoItemIdJson = JsonSerializer.Serialize(new { TodoItemId = todoItemId });
        var todoItemCreatedEventTypeName = typeof(TodoItemCreatedDomainEvent).FullName;

        var outboxMessage = await DbContext.OutboxMessages
            .Where(om => EF.Functions.JsonContains(om.Content, todoItemIdJson) &&
                         om.Type == todoItemCreatedEventTypeName)
            .SingleAsync();

        Assert.Null(outboxMessage.ProcessedOnUtc); // Not processed yet
        Assert.Equal(0, outboxMessage.AttemptCount);
    }

    [Fact]
    public async Task Create_WithoutDescription_ShouldCreateTodoItemWithNullDescription()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "create_no_desc_user",
            email: "create_no_desc@example.com");

        var command = new CreateTodoItemCommand(
            Title: "Todo Without Description",
            Description: null,
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            Labels: ["misc"]);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.ShouldBeRight();
        var todoItemId = result.RightValueOrThrow().TodoItemId;

        var createdItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.Null(createdItem.Description);
    }

    [Fact]
    public async Task Create_WithEmptyLabels_ShouldCreateTodoItemWithEmptyLabels()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "create_empty_labels_user",
            email: "create_empty_labels@example.com");

        var command = new CreateTodoItemCommand(
            Title: "Todo With Empty Labels",
            Description: "No labels for this one",
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            Labels: []);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.ShouldBeRight();
        var todoItemId = result.RightValueOrThrow().TodoItemId;

        var createdItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.NotNull(createdItem.Labels);
        Assert.Empty(createdItem.Labels);
    }

    #endregion

    #region Complete Tests

    [Fact]
    public async Task Complete_ShouldMarkAsCompletedAndCreateOutboxMessage()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "complete_todo_user",
            email: "complete_todo@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Todo to Complete");

        var command = new CompleteTodoItemCommand(TodoItemId: todoItemId);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.ShouldBeRight();

        // Verify todo item was marked as completed
        var completedItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.True(completedItem.IsCompleted);
        Assert.NotNull(completedItem.CompletedOnUtc);

        // Verify outbox message was created for TodoItemCompletedDomainEvent
        var todoItemIdJson = JsonSerializer.Serialize(new { TodoItemId = todoItemId });
        var todoItemCompletedEventTypeName = typeof(TodoItemCompletedDomainEvent).FullName;

        var outboxMessage = await DbContext.OutboxMessages
            .Where(om => EF.Functions.JsonContains(om.Content, todoItemIdJson) &&
                         om.Type == todoItemCompletedEventTypeName)
            .SingleAsync();

        Assert.Null(outboxMessage.ProcessedOnUtc); // Not processed yet
        Assert.Equal(0, outboxMessage.AttemptCount);
    }

    [Fact]
    public async Task Complete_AlreadyCompletedItem_ShouldFail()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "complete_twice_user",
            email: "complete_twice@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Todo to Complete Twice");

        // Complete once
        var firstComplete = await Sender.Send(new CompleteTodoItemCommand(todoItemId));
        firstComplete.RightValueOrThrow();

        // Act - Try to complete again
        var result = await Sender.Send(new CompleteTodoItemCommand(todoItemId));

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.AlreadyCompleted, left));

        // Verify the item is still completed
        var completedItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.True(completedItem.IsCompleted);
    }

    [Fact]
    public async Task Complete_NonExistentItem_ShouldFail()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "complete_nonexistent_user",
            email: "complete_nonexistent@example.com");

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await Sender.Send(new CompleteTodoItemCommand(nonExistentId));

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotFound, left));

        // Verify no item exists with that ID
        var item = await DbContext.TodoItems.SingleOrDefaultAsync(t => t.Id == nonExistentId);
        Assert.Null(item);
    }

    [Fact]
    public async Task Complete_ItemNotOwnedByUser_ShouldFail()
    {
        // Arrange - Create user 1 and their todo item
        var user1Id = await CreateAndAuthenticateUserAsync(
            username: "owner_user",
            email: "owner@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "User 1's Todo");

        // Switch to user 2
        var user2Id = await CreateAndAuthenticateUserAsync(
            username: "other_user",
            email: "other@example.com");

        Assert.NotEqual(user1Id, user2Id);

        // Act - User 2 tries to complete User 1's todo
        var result = await Sender.Send(new CompleteTodoItemCommand(todoItemId));

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotOwnedByUser, left));

        // Verify the item is still incomplete
        var item = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.False(item.IsCompleted);
    }

    #endregion

    #region MarkAsIncomplete Tests

    [Fact]
    public async Task MarkAsIncomplete_ShouldRevertCompletion()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "incomplete_user",
            email: "incomplete@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Todo to Revert");

        // Complete the item first
        var completeResult = await Sender.Send(new CompleteTodoItemCommand(todoItemId));
        completeResult.RightValueOrThrow();

        // Verify it's completed
        var completedItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.True(completedItem.IsCompleted);
        Assert.NotNull(completedItem.CompletedOnUtc);

        // Act - Mark as incomplete
        var result = await Sender.Send(new MarkAsIncompleteTodoItemCommand(todoItemId));

        // Assert
        result.ShouldBeRight();

        // Refresh from database
        await DbContext.Entry(completedItem).ReloadAsync();
        Assert.False(completedItem.IsCompleted);
        Assert.Null(completedItem.CompletedOnUtc);
    }

    [Fact]
    public async Task MarkAsIncomplete_NotCompletedItem_ShouldFail()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "mark_incomplete_fail_user",
            email: "mark_incomplete_fail@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Already Incomplete Todo");

        // Act - Try to mark as incomplete when it's already incomplete
        var result = await Sender.Send(new MarkAsIncompleteTodoItemCommand(todoItemId));

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotCompleted, left));

        // Verify the item is still incomplete
        var item = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.False(item.IsCompleted);
        Assert.Null(item.CompletedOnUtc);
    }

    [Fact]
    public async Task MarkAsIncomplete_ItemNotOwnedByUser_ShouldFail()
    {
        // Arrange - Create user 1 and their completed todo item
        await CreateAndAuthenticateUserAsync(
            username: "incomplete_owner",
            email: "incomplete_owner@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Completed Todo to Steal");

        // Complete the item
        var completeResult = await Sender.Send(new CompleteTodoItemCommand(todoItemId));
        completeResult.RightValueOrThrow();

        // Switch to user 2
        await CreateAndAuthenticateUserAsync(
            username: "incomplete_thief",
            email: "incomplete_thief@example.com");

        // Act - User 2 tries to mark User 1's todo as incomplete
        var result = await Sender.Send(new MarkAsIncompleteTodoItemCommand(todoItemId));

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotOwnedByUser, left));

        // Verify the item is still completed
        var item = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.True(item.IsCompleted);
        Assert.NotNull(item.CompletedOnUtc);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateTodoItem()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "update_user",
            email: "update@example.com");

        var todoItemId = await CreateTodoItemAsync(
            title: "Original Title",
            description: "Original Description",
            labels: ["original"]);

        var updateCommand = new UpdateTodoItemCommand(
            TodoItemId: todoItemId,
            Title: "Updated Title",
            Description: "Updated Description",
            DueDate: DateTimeOffset.UtcNow.AddDays(14),
            Labels: ["updated", "important"]);

        // Act
        var result = await Sender.Send(updateCommand);

        // Assert
        result.ShouldBeRight();

        var updatedItem = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.Equal("Updated Title", updatedItem.Title.Value);
        Assert.Equal("Updated Description", updatedItem.Description?.Value);
        Assert.Equal(2, updatedItem.Labels.Count);
        Assert.Contains("updated", updatedItem.Labels);
        Assert.Contains("important", updatedItem.Labels);
        Assert.Equal(updateCommand.DueDate, updatedItem.DueDateOnUtc);
    }

    [Fact]
    public async Task Update_CompletedItem_ShouldFail()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "update_completed_user",
            email: "update_completed@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "Todo to Complete Then Update");

        // Complete the item
        var completeResult = await Sender.Send(new CompleteTodoItemCommand(todoItemId));
        completeResult.RightValueOrThrow();

        var updateCommand = new UpdateTodoItemCommand(
            TodoItemId: todoItemId,
            Title: "Trying to Update Completed Item",
            Description: "This should fail",
            DueDate: DateTimeOffset.UtcNow.AddDays(7),
            Labels: []);

        // Act
        var result = await Sender.Send(updateCommand);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem, left));

        // Verify the item was not updated
        var item = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.Equal("Todo to Complete Then Update", item.Title.Value);
    }

    [Fact]
    public async Task Update_NonExistentItem_ShouldFail()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "update_nonexistent_user",
            email: "update_nonexistent@example.com");

        var nonExistentId = Guid.NewGuid();

        var updateCommand = new UpdateTodoItemCommand(
            TodoItemId: nonExistentId,
            Title: "Update Non-Existent",
            Description: null,
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            Labels: []);

        // Act
        var result = await Sender.Send(updateCommand);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotFound, left));
    }

    [Fact]
    public async Task Update_ItemNotOwnedByUser_ShouldFail()
    {
        // Arrange - Create user 1 and their todo item
        await CreateAndAuthenticateUserAsync(
            username: "update_owner",
            email: "update_owner@example.com");

        var todoItemId = await CreateTodoItemAsync(title: "User 1's Todo to Update");

        // Switch to user 2
        await CreateAndAuthenticateUserAsync(
            username: "update_thief",
            email: "update_thief@example.com");

        var updateCommand = new UpdateTodoItemCommand(
            TodoItemId: todoItemId,
            Title: "Stolen Update",
            Description: null,
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            Labels: []);

        // Act - User 2 tries to update User 1's todo
        var result = await Sender.Send(updateCommand);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotOwnedByUser, left));

        // Verify the item was not updated
        var item = await DbContext.TodoItems.SingleAsync(t => t.Id == todoItemId);
        Assert.Equal("User 1's Todo to Update", item.Title.Value);
    }

    #endregion

    #region GetTodoItems Tests

    [Fact]
    public async Task GetTodoItems_ShouldReturnOnlyUserItems()
    {
        // Arrange - Create user 1 with 2 todo items
        var user1Id = await CreateAndAuthenticateUserAsync(
            username: "get_user_1",
            email: "get_user_1@example.com");

        var user1TodoId1 = await CreateTodoItemAsync(title: "User 1 - Todo 1");
        var user1TodoId2 = await CreateTodoItemAsync(title: "User 1 - Todo 2");

        // Create user 2 with 1 todo item
        await CreateAndAuthenticateUserAsync(
            username: "get_user_2",
            email: "get_user_2@example.com");

        var user2TodoId = await CreateTodoItemAsync(title: "User 2 - Todo 1");

        // Act - Get user 2's items
        var user2Result = await Sender.Send(new GetTodoItemsQuery());

        // Assert - User 2 should only see their own item
        user2Result.ShouldBeRight(r =>
        {
            var item = Assert.Single(r.TodoItems);
            Assert.Equal(user2TodoId, item.Id);
            Assert.Equal("User 2 - Todo 1", item.Title);
        });

        // Switch back to user 1
        TestUserContext.SetCurrentUser(user1Id);

        // Act - Get user 1's items
        var user1Result = await Sender.Send(new GetTodoItemsQuery());

        // Assert - User 1 should only see their own items
        user1Result.ShouldBeRight(r =>
        {
            Assert.Equal(2, r.TodoItems.Count);
            Assert.Contains(r.TodoItems, t => t.Id == user1TodoId1);
            Assert.Contains(r.TodoItems, t => t.Id == user1TodoId2);
            Assert.DoesNotContain(r.TodoItems, t => t.Id == user2TodoId);
        });
    }

    [Fact]
    public async Task GetTodoItems_WithNoItems_ShouldReturnEmptyList()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "get_empty_user",
            email: "get_empty@example.com");

        // Act
        var result = await Sender.Send(new GetTodoItemsQuery());

        // Assert
        result.ShouldBeRight(r => Assert.Empty(r.TodoItems));
    }

    [Fact]
    public async Task GetTodoItems_ShouldReturnCorrectResponseProperties()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(
            username: "get_props_user",
            email: "get_props@example.com");

        var todoItemId = await CreateTodoItemAsync(
            title: "Detailed Todo",
            description: "Detailed Description",
            daysFromNow: 5,
            labels: ["label1", "label2"]);

        // Complete the item
        var completeResult = await Sender.Send(new CompleteTodoItemCommand(todoItemId));
        completeResult.RightValueOrThrow();

        // Act
        var result = await Sender.Send(new GetTodoItemsQuery());

        // Assert
        result.ShouldBeRight(r =>
        {
            var item = Assert.Single(r.TodoItems);

            Assert.Equal(todoItemId, item.Id);
            Assert.Equal("Detailed Todo", item.Title);
            Assert.Equal("Detailed Description", item.Description);
            Assert.True(item.IsCompleted);
            Assert.NotNull(item.CompletedOnUtc);
            Assert.Equal(2, item.Labels.Count);
            Assert.Contains("label1", item.Labels);
            Assert.Contains("label2", item.Labels);
        });
    }

    #endregion
}