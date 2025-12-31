using FluentValidation.TestHelper;
using NetAuth.Application.TodoItems;
using NetAuth.Application.TodoItems.Complete;

namespace NetAuth.UnitTests.Application.TodoItems.Complete;

public class CompleteTodoItemCommandValidatorTests
{
    private readonly CompleteTodoItemCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenTodoItemIdIsEmpty()
    {
        // Arrange
        var command = new CompleteTodoItemCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TodoItemId)
            .WithErrorMessage(TodoItemValidationErrors.CompleteTodoItem.IdIsRequired.Message)
            .Only();
    }

    [Fact]
    public void ShouldNotHaveError_WhenTodoItemIdIsNotEmpty()
    {
        // Arrange
        var command = new CompleteTodoItemCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}