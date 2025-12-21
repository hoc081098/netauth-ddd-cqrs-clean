using LanguageExt.UnitTesting;
using NetAuth.Domain.TodoItems;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.TodoItems;

public class TodoTitleTests
{
    [Theory]
    [InlineData("Buy groceries")]
    [InlineData("Complete project")]
    [InlineData("Read book")]
    [InlineData("Call mom")]
    [InlineData("Fix bug #123")]
    [InlineData("Meeting @ 3pm")]
    [InlineData("TODO: review PR")]
    [InlineData("a")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("Short")]
    [InlineData("Very Long Title With Multiple Words And Numbers 123")]
    public void Create_WithValidTitle_ShouldReturnSuccess(string validTitle)
    {
        // Act
        var result = TodoTitle.Create(validTitle);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(validTitle, right.Value);
            Assert.Equal(validTitle, (string)right); // Test implicit conversion
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  ")]
    public void Create_WithNullOrWhiteSpace_ShouldReturnNullOrEmptyError(string? invalidTitle)
    {
        // Act
        var result = TodoTitle.Create(invalidTitle!);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Title.NullOrEmpty, left));
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        var longTitle = new string('a', TodoTitle.MaxLength + 1);

        // Act
        var result = TodoTitle.Create(longTitle);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(TodoItemDomainErrors.Title.TooLong, left));
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxLengthTitle = new string('a', TodoTitle.MaxLength);

        // Act
        var result = TodoTitle.Create(maxLengthTitle);

        // Assert
        result.ShouldBeRight(right => Assert.Equal(maxLengthTitle, right.Value));
    }

    [Fact]
    public void Equals_WithSameTitleValue_ShouldReturnTrue()
    {
        // Arrange
        var title1Result = TodoTitle.Create("Buy groceries");
        var title2Result = TodoTitle.Create("Buy groceries");

        // Act & Assert
        title1Result.ShouldBeRight(title1 =>
        {
            title2Result.ShouldBeRight(title2 =>
            {
                Assert.Equal(title1, title2);
                Assert.True(title1.Equals(title2));
                Assert.True(title1.Equals((object?)title2));
                Assert.Equal(title1.GetHashCode(), title2.GetHashCode());
            });
        });
    }

    [Fact]
    public void Equals_WithDifferentTitleValues_ShouldReturnFalse()
    {
        // Arrange
        var title1Result = TodoTitle.Create("Buy groceries");
        var title2Result = TodoTitle.Create("Complete project");

        // Act & Assert
        title1Result.ShouldBeRight(title1 =>
        {
            title2Result.ShouldBeRight(title2 =>
            {
                Assert.NotEqual(title1, title2);
                Assert.False(title1.Equals(title2));
                Assert.False(title1.Equals((object?)title2));
            });
        });
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var titleText = "Buy groceries";
        var titleResult = TodoTitle.Create(titleText);

        // Act & Assert
        titleResult.ShouldBeRight(title =>
        {
            string converted = title;
            Assert.Equal(titleText, converted);
        });
    }
}

