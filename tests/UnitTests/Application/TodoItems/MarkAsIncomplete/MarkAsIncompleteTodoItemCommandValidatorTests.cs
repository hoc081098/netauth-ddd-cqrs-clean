using FluentValidation.TestHelper;
using NetAuth.Application.TodoItems;
using NetAuth.Application.TodoItems.MarkAsIncomplete;

namespace NetAuth.UnitTests.Application.TodoItems.MarkAsIncomplete;

public class MarkAsIncompleteTodoItemCommandValidatorTests
{
    private readonly MarkAsIncompleteTodoItemCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenTodoItemIdIsEmpty()
    {
        // Arrange
        var command = new MarkAsIncompleteTodoItemCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TodoItemId)
            .WithErrorMessage(TodoItemValidationErrors.MarkAsIncompleteTodoItem.IdIsRequired.Message)
            .Only();
    }

    [Fact]
    public void ShouldNotHaveError_WhenTodoItemIdIsNotEmpty()
    {
        // Arrange
        var command = new MarkAsIncompleteTodoItemCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}