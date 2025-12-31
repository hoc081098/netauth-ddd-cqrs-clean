using FluentValidation.Results;
using NetAuth.Application.Core.Errors;
using NetAuth.Domain.Core.Primitives;

// ReSharper disable CollectionNeverUpdated.Local

namespace NetAuth.UnitTests.Application.Core.Errors;

public class ValidationErrorTests
{
    [Fact]
    public void Constructor_ShouldSetBaseProperties()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Email", errorMessage: "Email is required.")
            {
                ErrorCode = "Email.Required"
            }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.Equal("General.ValidationError", error.Code);
        Assert.Equal("One or more validation errors occurred.", error.Message);
        Assert.Equal(DomainError.ErrorType.Validation, error.Type);
    }

    [Fact]
    public void Constructor_ShouldConvertValidationFailuresToDomainErrors()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Email", errorMessage: "Email is required.")
            {
                ErrorCode = "Email.Required"
            },
            new(propertyName: "Password", errorMessage: "Password must be at least 8 characters.")
            {
                ErrorCode = "Password.TooShort"
            }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.Equal(2, error.Errors.Count);

        Assert.Contains(error.Errors, e =>
            e is
            {
                Code: "Email.Required",
                Message: "Email is required.",
                Type: DomainError.ErrorType.Validation
            });

        Assert.Contains(error.Errors, e =>
            e is
            {
                Code: "Password.TooShort",
                Message: "Password must be at least 8 characters.",
                Type: DomainError.ErrorType.Validation
            });
    }

    [Fact]
    public void Constructor_ShouldRemoveDuplicateErrors()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Email", errorMessage: "Email is required.")
            {
                ErrorCode = "Email.Required"
            },
            new(propertyName: "Email", errorMessage: "Email is required.")
            {
                ErrorCode = "Email.Required"
            }, // Duplicate
            new(propertyName: "Password", errorMessage: "Password is required.")
            {
                ErrorCode = "Password.Required"
            }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.Equal(2, error.Errors.Count);
    }

    [Fact]
    public void Constructor_WithSingleFailure_ShouldWork()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Name", errorMessage: "Name is required.")
            {
                ErrorCode = "Name.Required"
            }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        var single = Assert.Single(error.Errors);
        Assert.Equal("Name.Required", single.Code);
        Assert.Equal("Name is required.", single.Message);
    }

    [Fact]
    public void Constructor_WithEmptyFailures_ShouldThrowException()
    {
        // Arrange
        var failures = new List<ValidationFailure>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationError(failures));
    }

    [Fact]
    public void Constructor_WithNullFailures_ShouldThrowException()
    {
        // Arrange
        IEnumerable<ValidationFailure> failures = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationError(failures));
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Email", errorMessage: "Email is required.") { ErrorCode = "Email.Required" }
        ];

        var error = new ValidationError(failures);
        var errors = error.Errors;

        // Act & Assert - The Errors property should be IReadOnlyList
        Assert.IsType<IReadOnlyList<DomainError>>(errors, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() =>
            ((IList<DomainError>)errors).Add(
                new DomainError(
                    code: "Password.Required",
                    message: "Password is required.",
                    type: DomainError.ErrorType.Validation)
            )
        );
    }

    [Fact]
    public void ValidationError_ShouldInheritFromDomainError()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Email", errorMessage: "Email is required.") { ErrorCode = "Email.Required" }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.IsType<DomainError>(error, exactMatch: false);
    }

    [Fact]
    public void Errors_AllShouldHaveValidationType()
    {
        // Arrange
        List<ValidationFailure> failures =
        [
            new(propertyName: "Field1", errorMessage: "Error 1") { ErrorCode = "Error1" },
            new(propertyName: "Field2", errorMessage: "Error 2") { ErrorCode = "Error2" },
            new(propertyName: "Field3", errorMessage: "Error 3") { ErrorCode = "Error3" }
        ];

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.All(error.Errors,
            e => Assert.Equal(DomainError.ErrorType.Validation, e.Type));
    }

    [Fact]
    public void Constructor_WithComplexErrorMessages_ShouldPreserveMessages()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(
                propertyName: "Email",
                errorMessage: "The email 'test@' is not a valid email address.")
            {
                ErrorCode = "Email.InvalidFormat"
            },
            new(
                propertyName: "Password",
                errorMessage:
                "Password must contain at least one uppercase letter, one lowercase letter, and one number.")
            {
                ErrorCode = "Password.WeakPassword"
            }
        };

        // Act
        var error = new ValidationError(failures);

        // Assert
        Assert.Contains(error.Errors, e =>
            e.Message == "The email 'test@' is not a valid email address.");
        Assert.Contains(error.Errors, e =>
            e.Message == "Password must contain at least one uppercase letter, one lowercase letter, and one number.");
    }
}