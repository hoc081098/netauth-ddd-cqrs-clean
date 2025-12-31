using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.TodoItems.Update;
using NetAuth.Domain.TodoItems;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.TodoItems;
using NSubstitute;

namespace NetAuth.UnitTests.Application.TodoItems.Update;

public class UpdateTodoItemCommandHandlerTests
{
    // Subject under test (SUT)
    private readonly UpdateTodoItemCommandHandler _handler;

    // Dependencies
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Test data
    private static readonly Guid UserId = TodoItemTestData.UserId;
    private static readonly DateTimeOffset CurrentUtc = TodoItemTestData.CurrentUtc;

    public UpdateTodoItemCommandHandlerTests()
    {
        _todoItemRepository = Substitute.For<ITodoItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        _handler = new UpdateTodoItemCommandHandler(
            _todoItemRepository,
            _unitOfWork,
            FixedClock.CreateWithUtcNow(CurrentUtc),
            userContext);
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ShouldReturnDomainError()
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

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: "",
            Description: "Updated Description",
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.Title.NullOrEmpty, left));

        await _todoItemRepository.DidNotReceive()
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenTodoItemDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var todoItemId = Guid.NewGuid();
        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItemId,
            Title: "Updated Title",
            Description: "Updated Description",
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

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

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

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
    public async Task Handle_WhenTodoItemIsCompleted_ShouldReturnCannotUpdateCompletedItemError()
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

        // Mark as completed
        todoItem.MarkAsCompleted(CurrentUtc);

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem, left));

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithPastDueDate_ShouldReturnDomainError()
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

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            DueDate: CurrentUtc.AddDays(-1),
            Labels: TodoItemTestData.NonEmptyLabels);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateTodoItemAndSaveChanges()
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

        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newDueDate = CurrentUtc.AddDays(5);
        var newLabels = new[] { "label1", "label2" };

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: newTitle,
            Description: newDescription,
            DueDate: newDueDate,
            Labels: newLabels);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight();
        Assert.Equal(newTitle, todoItem.Title.Value);
        Assert.Equal(newDescription, todoItem.Description?.Value);
        Assert.Equal(newDueDate.ToUniversalTime(), todoItem.DueDateOnUtc);
        Assert.Equal(newLabels, todoItem.Labels);

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldUpdateTodoItemSuccessfully()
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

        var command = new UpdateTodoItemCommand(
            TodoItemId: todoItem.Id,
            Title: "Updated Title",
            Description: null,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        _todoItemRepository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TodoItem?>(todoItem));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight();
        Assert.Null(todoItem.Description);

        await _todoItemRepository.Received(1)
            .GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}