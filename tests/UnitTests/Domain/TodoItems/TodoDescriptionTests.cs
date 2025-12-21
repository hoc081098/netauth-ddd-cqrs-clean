using LanguageExt;
using LanguageExt.UnitTesting;
using NetAuth.Domain.TodoItems;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.TodoItems;

public class TodoDescriptionTests
{
    [Theory]
    [InlineData("This is a simple description")]
    [InlineData("Detailed instructions for the task")]
    [InlineData("a")]
    [InlineData("Short")]
    [InlineData("Description with numbers 123 and symbols !@#$%")]
    [InlineData("Multi-line\ndescription\nwith breaks")]
    [InlineData("")]
    [InlineData("   ")] // Whitespace is allowed for descriptions
    public void Create_WithValidDescription_ShouldReturnSuccess(string validDescription)
    {
        // Act
        var result = TodoDescription.Create(validDescription);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(validDescription, right.Value);
            Assert.Equal(validDescription, (string)right); // Test implicit conversion
        });
    }

    [Fact]
    public void Create_WithNull_ShouldReturnNullError()
    {
        // Act
        var result = TodoDescription.Create(null!);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Description.Null, left));
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        var longDescription = new string('a', TodoDescription.MaxLength + 1);

        // Act
        var result = TodoDescription.Create(longDescription);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Description.TooLong, left));
    }

    [Fact]
    public void Create_WithDescriptionAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxLengthDescription = new string('a', TodoDescription.MaxLength);

        // Act
        var result = TodoDescription.Create(maxLengthDescription);

        // Assert
        result.ShouldBeRight(right => Assert.Equal(maxLengthDescription, right.Value));
    }

    [Theory]
    [InlineData("This is a description")]
    [InlineData("Another description")]
    [InlineData("")]
    public void CreateOption_WithValidDescription_ShouldReturnSome(string validDescription)
    {
        // Act
        var result = TodoDescription.CreateOption(validDescription);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.True(right.IsSome);
            right.IfSome(description => Assert.Equal(validDescription, description.Value));
        });
    }

    [Fact]
    public void CreateOption_WithNull_ShouldReturnNone()
    {
        // Act
        var result = TodoDescription.CreateOption(null);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.True(right.IsNone);
            Assert.Equal(Option<TodoDescription>.None, right);
        });
    }

    [Fact]
    public void CreateOption_WithDescriptionExceedingMaxLength_ShouldReturnError()
    {
        // Arrange
        var longDescription = new string('a', TodoDescription.MaxLength + 1);

        // Act
        var result = TodoDescription.CreateOption(longDescription);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Description.TooLong, left));
    }

    [Fact]
    public void Equals_WithSameDescriptionValue_ShouldReturnTrue()
    {
        // Arrange
        var description1Result = TodoDescription.Create("This is a description");
        var description2Result = TodoDescription.Create("This is a description");

        // Act & Assert
        description1Result.ShouldBeRight(description1 =>
        {
            description2Result.ShouldBeRight(description2 =>
            {
                Assert.Equal(description1, description2);
                Assert.True(description1.Equals(description2));
                Assert.True(description1.Equals((object?)description2));
                Assert.Equal(description1.GetHashCode(), description2.GetHashCode());
            });
        });
    }

    [Fact]
    public void Equals_WithDifferentDescriptionValues_ShouldReturnFalse()
    {
        // Arrange
        var description1Result = TodoDescription.Create("First description");
        var description2Result = TodoDescription.Create("Second description");

        // Act & Assert
        description1Result.ShouldBeRight(description1 =>
        {
            description2Result.ShouldBeRight(description2 =>
            {
                Assert.NotEqual(description1, description2);
                Assert.False(description1.Equals(description2));
                Assert.False(description1.Equals((object?)description2));
            });
        });
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var descriptionText = "This is a description";
        var descriptionResult = TodoDescription.Create(descriptionText);

        // Act & Assert
        descriptionResult.ShouldBeRight(description =>
        {
            string converted = description;
            Assert.Equal(descriptionText, converted);
        });
    }
}

