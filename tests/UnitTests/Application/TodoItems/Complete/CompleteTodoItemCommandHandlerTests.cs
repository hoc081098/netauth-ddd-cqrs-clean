using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.TodoItems.Complete;
using NetAuth.Domain.TodoItems;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.TodoItems;
using NSubstitute;

namespace NetAuth.UnitTests.Application.TodoItems.Complete;

public class CompleteTodoItemCommandHandlerTests
{
    // Subject under test (SUT)
    private readonly CompleteTodoItemCommandHandler _handler;

    // Dependencies
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Test data
    private static readonly Guid UserId = TodoItemTestData.UserId;
    private static readonly DateTimeOffset CurrentUtc = TodoItemTestData.CurrentUtc;

    public CompleteTodoItemCommandHandlerTests()
    {
        _todoItemRepository = Substitute.For<ITodoItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        _handler = new CompleteTodoItemCommandHandler(
            _todoItemRepository,
            _unitOfWork,
            FixedClock.CreateWithUtcNow(CurrentUtc),
            userContext);
    }

    [Fact]
    public async Task Handle_WhenTodoItemDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var todoItemId = Guid.NewGuid();
        var command = new CompleteTodoItemCommand(todoItemId);

        _todoItemRepository.GetByIdAsync(todoItemId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotFound, left));

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItemId, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenTodoItemIsNotOwnedByUser_ShouldReturnNotOwnedByUserError()
    {
        // Arrange
        var differentUserId = Guid.NewGuid();
        var todoItem = TodoItem.Create(
            userId: differentUserId,
            title: TodoItemTestData.Title.Value,
            description: TodoItemTestData.Description.Value,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: CurrentUtc
        ).RightValueOrThrow();

        var command = new CompleteTodoItemCommand(todoItem.Id);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.NotOwnedByUser, left));

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenTodoItemIsValid_ShouldMarkAsCompletedAndSaveChanges()
    {
        // Arrange
        var todoItem = TodoItem.Create(
            userId: UserId,
            title: TodoItemTestData.Title.Value,
            description: TodoItemTestData.Description.Value,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: CurrentUtc
        ).RightValueOrThrow();

        var command = new CompleteTodoItemCommand(todoItem.Id);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight();
        Assert.True(todoItem.IsCompleted);
        Assert.Equal(CurrentUtc, todoItem.CompletedOnUtc);

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTodoItemIsAlreadyCompleted_ShouldReturnAlreadyCompletedError()
    {
        // Arrange
        var todoItem = TodoItem.Create(
            userId: UserId,
            title: TodoItemTestData.Title.Value,
            description: TodoItemTestData.Description.Value,
            dueDateOnUtc: TodoItemTestData.FutureDueDate,
            labels: TodoItemTestData.NonEmptyLabels,
            currentUtc: CurrentUtc
        ).RightValueOrThrow();

        // Mark as completed first
        todoItem.MarkAsCompleted(CurrentUtc).ShouldBeRight();
        Assert.True(todoItem.IsCompleted);
        Assert.Equal(CurrentUtc, todoItem.CompletedOnUtc);

        var command = new CompleteTodoItemCommand(todoItem.Id);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.AlreadyCompleted, left));
        // The state should remain unchanged
        Assert.True(todoItem.IsCompleted);
        Assert.Equal(CurrentUtc, todoItem.CompletedOnUtc);

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }
}