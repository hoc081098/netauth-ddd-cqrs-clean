using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.TodoItems.Get;
using NetAuth.Domain.TodoItems;
using NetAuth.UnitTests.Domain.TodoItems;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.TodoItems.Get;

public class GetTodoItemsQueryHandlerTests
{
    // Subject under test (SUT)
    private readonly GetTodoItemsQueryHandler _handler;

    // Dependencies
    private readonly ITodoItemRepository _todoItemRepository;

    // Test data
    private readonly Guid _userId = TodoItemTestData.UserId;
    private readonly DateTimeOffset _currentUtc = TodoItemTestData.CurrentUtc;

    private static readonly GetTodoItemsQuery Query = new();

    public GetTodoItemsQueryHandlerTests()
    {
        _todoItemRepository = Substitute.For<ITodoItemRepository>();
        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(_userId);

        _handler = new GetTodoItemsQueryHandler(
            _todoItemRepository,
            userContext);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoTodoItems_ShouldReturnEmptyList()
    {
        // Arrange
        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => { Assert.Empty(right.TodoItems); });

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHasTodoItems_ShouldReturnAllTodoItems()
    {
        // Arrange
        var todoItem1 = TodoItem.Create(
            userId: _userId,
            title: "Todo 1",
            description: "Description 1",
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: ["label1"],
            currentUtc: _currentUtc
        ).RightValueOrThrow();

        var todoItem2 = TodoItem.Create(
            userId: _userId,
            title: "Todo 2",
            description: null,
            dueDateOnUtc: TodoItemTestData.FutureDueDate.AddDays(1),
            labels: ["label2", "label3"],
            currentUtc: _currentUtc
        ).RightValueOrThrow();

        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TodoItem>>([todoItem1, todoItem2]));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(2, right.TodoItems.Count);

            var response1 = right.TodoItems[0];
            Assert.Equal(todoItem1.Id, response1.Id);
            Assert.Equal("Todo 1", response1.Title);
            Assert.Equal("Description 1", response1.Description);
            Assert.False(response1.IsCompleted);
            Assert.Null(response1.CompletedOnUtc);
            Assert.Equal(todoItem1.DueDateOnUtc, response1.DueDateOnUtc);
            Assert.Single(response1.Labels);
            Assert.Equal("label1", response1.Labels[0]);

            var response2 = right.TodoItems[1];
            Assert.Equal(todoItem2.Id, response2.Id);
            Assert.Equal("Todo 2", response2.Title);
            Assert.Null(response2.Description);
            Assert.False(response2.IsCompleted);
            Assert.Null(response2.CompletedOnUtc);
            Assert.Equal(todoItem2.DueDateOnUtc, response2.DueDateOnUtc);
            Assert.Equal(2, response2.Labels.Count);
        });

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTodoItemIsCompleted_ShouldIncludeCompletedOnUtc()
    {
        // Arrange
        var todoItem = TodoItem.Create(
            userId: _userId,
            title: TodoItemTestData.Title.Value,
            description: TodoItemTestData.Description.Value,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: _currentUtc
        ).RightValueOrThrow();

        // Mark as completed
        todoItem.MarkAsCompleted(_currentUtc);

        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TodoItem>>([todoItem]));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Single(right.TodoItems);
            var response = right.TodoItems[0];
            Assert.True(response.IsCompleted);
            Assert.Equal(_currentUtc, response.CompletedOnUtc);
        });

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMapTodoItemPropertiesCorrectly()
    {
        // Arrange
        var todoItem = TodoItem.Create(
            userId: _userId,
            title: TodoItemTestData.Title.Value,
            description: TodoItemTestData.Description.Value,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: _currentUtc
        ).RightValueOrThrow();

        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TodoItem>>([todoItem]));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Single(right.TodoItems);
            var response = right.TodoItems[0];

            Assert.Equal(todoItem.Id, response.Id);
            Assert.Equal(todoItem.Title.Value, response.Title);
            Assert.Equal(todoItem.Description!.Value, response.Description);
            Assert.Equal(todoItem.IsCompleted, response.IsCompleted);
            Assert.Equal(todoItem.CompletedOnUtc, response.CompletedOnUtc);
            Assert.Equal(todoItem.DueDateOnUtc, response.DueDateOnUtc);
            Assert.Equal(todoItem.Labels, response.Labels);
            Assert.Equal(todoItem.CreatedOnUtc, response.CreatedOnUtc);
        });

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDescriptionIsNull_ShouldReturnNull()
    {
        // Arrange
        var todoItem = TodoItem.Create(
            userId: _userId,
            title: TodoItemTestData.Title.Value,
            description: null,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: _currentUtc
        ).RightValueOrThrow();

        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TodoItem>>([todoItem]));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Single(right.TodoItems);
            Assert.Null(right.TodoItems[0].Description);
        });

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCallToRepositoryFails()
    {
        // Arrange
        _todoItemRepository.GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(Query, CancellationToken.None));

        await _todoItemRepository.Received(1)
            .GetTodoItemsByUserId(_userId, Arg.Any<CancellationToken>());
    }
}