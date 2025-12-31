using FluentValidation.TestHelper;
using NetAuth.Application.TodoItems;
using NetAuth.Application.TodoItems.Update;
using NetAuth.Domain.TodoItems;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.TodoItems;

namespace NetAuth.UnitTests.Application.TodoItems.Update;

public class UpdateTodoItemCommandValidatorTests
{
    private static readonly DateTimeOffset CurrentUtc = TodoItemTestData.CurrentUtc;

    private readonly UpdateTodoItemCommandValidator _validator;

    public UpdateTodoItemCommandValidatorTests()
    {
        _validator = new UpdateTodoItemCommandValidator(
            FixedClock.CreateWithUtcNow(CurrentUtc));
    }

    [Fact]
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: "",
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.TitleIsRequired.Message)
            .Only();
    }

    [Fact]
    public void ShouldHaveError_WhenTitleIsTooLong()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: new string('a', TodoTitle.MaxLength + 1),
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.TitleTooLong.Message)
            .Only();
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionIsTooLong()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: new string('a', TodoDescription.MaxLength + 1),
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.DescriptionTooLong.Message)
            .Only();
    }

    [Fact]
    public void ShouldNotHaveError_WhenDescriptionIsNull()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: null,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenDueDateIsEmpty()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: default,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.DueDateIsRequired.Message);
    }

    [Fact]
    public void ShouldHaveError_WhenDueDateIsInPast()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: CurrentUtc.AddDays(-1),
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.DueDateMustBeTodayOrLater.Message)
            .Only();
    }

    [Fact]
    public void ShouldNotHaveError_WhenDueDateIsToday()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: CurrentUtc,
            Labels: TodoItemTestData.NonEmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenLabelsIsNull()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Labels)
            .WithErrorMessage(TodoItemValidationErrors.UpdateTodoItem.LabelsIsRequired.Message)
            .Only();
    }

    [Fact]
    public void ShouldNotHaveError_WhenLabelsIsEmpty()
    {
        // Arrange
        var command = new UpdateTodoItemCommand(
            TodoItemId: Guid.NewGuid(),
            Title: TodoItemTestData.Title.Value,
            Description: TodoItemTestData.Description.Value,
            DueDate: TodoItemTestData.FutureDueDate,
            Labels: TodoItemTestData.EmptyLabels);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}