using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.TodoItems.Create;
using NetAuth.Domain.TodoItems;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.TodoItems;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.TodoItems.Create;

public class CreateTodoItemCommandHandlerTests
{
    // Subject under test (SUT)
    private readonly CreateTodoItemCommandHandler _handler;

    // Dependencies
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Test data
    private readonly Guid _userId = TodoItemTestData.UserId;
    private readonly DateTimeOffset _currentUtc = TodoItemTestData.CurrentUtc;

    public CreateTodoItemCommandHandlerTests()
    {
        _todoItemRepository = Substitute.For<ITodoItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(_userId);

        _handler = new CreateTodoItemCommandHandler(
            _todoItemRepository,
            userContext,
            _unitOfWork,
            FixedClock.CreateWithUtcNow(_currentUtc));
    }

    private async Task AssertInsertIsNotCalled()
    {
        _todoItemRepository.DidNotReceiveWithAnyArgs()
            .Insert(null!);

        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateTodoItemAndReturnId()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => Assert.NotEqual(Guid.Empty, right.TodoItemId));

        _todoItemRepository.Received(1)
            .Insert(
                Arg.Is<TodoItem>(todoItem =>
                    todoItem.Title == TodoItemTestData.Title &&
                    todoItem.Description == TodoItemTestData.Description &&
                    todoItem.DueDateOnUtc == TodoItemTestData.FutureDueDate &&
                    todoItem.Labels.SequenceEqual(TodoItemTestData.NonEmptyLabels)
                )
            );
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldCreateTodoItemSuccessfully()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: TodoItemTestData.Title.Value,
            Description: null,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => Assert.NotEqual(Guid.Empty, right.TodoItemId));

        _todoItemRepository.Received(1)
            .Insert(
                Arg.Is<TodoItem>(todoItem =>
                    todoItem.Title == TodoItemTestData.Title &&
                    todoItem.Description == null &&
                    todoItem.DueDateOnUtc == TodoItemTestData.FutureDueDate &&
                    todoItem.Labels.SequenceEqual(TodoItemTestData.NonEmptyLabels)
                )
            );
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ShouldReturnDomainError()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: "",
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.Title.NullOrEmpty, left));

        await AssertInsertIsNotCalled();
    }

    [Fact]
    public async Task Handle_WithTitleTooLong_ShouldReturnDomainError()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: new string('a', TodoTitle.MaxLength + 1),
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.Title.TooLong, left));

        await AssertInsertIsNotCalled();
    }

    [Fact]
    public async Task Handle_WithDescriptionTooLong_ShouldReturnDomainError()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: TodoItemTestData.Title.Value,
            Description: new string('a', TodoDescription.MaxLength + 1),
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.Description.TooLong, left));

        await AssertInsertIsNotCalled();
    }

    [Fact]
    public async Task Handle_WithPastDueDate_ShouldReturnDomainError()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: _currentUtc.AddDays(-1),
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(TodoItemDomainErrors.TodoItem.DueDateInPast, left));

        await AssertInsertIsNotCalled();
    }

    [Fact]
    public async Task Handle_WithEmptyLabels_ShouldCreateTodoItemSuccessfully()
    {
        // Arrange
        var command = new CreateTodoItemCommand(
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.EmptyLabels);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => Assert.NotEqual(Guid.Empty, right.TodoItemId));

        _todoItemRepository.Received(1)
            .Insert(
                Arg.Is<TodoItem>(todoItem =>
                    todoItem.Title == TodoItemTestData.Title &&
                    todoItem.Description == TodoItemTestData.Description &&
                    todoItem.DueDateOnUtc == TodoItemTestData.FutureDueDate &&
                    !todoItem.Labels.Any()
                )
            );
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}