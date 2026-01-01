using System.Diagnostics.CodeAnalysis;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.UnitTests.Domain.Core.Primitives;

[SuppressMessage("ReSharper", "AppendToCollectionExpression")]
[SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity")]
[SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code")]
public class DomainErrorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string code = "User.NotFound";
        const string message = "User was not found.";
        const DomainError.ErrorType type = DomainError.ErrorType.NotFound;

        // Act
        var error = new DomainError(code, message, type);

        // Assert
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(type, error.Type);
    }

    [Theory]
    [InlineData(DomainError.ErrorType.Validation)]
    [InlineData(DomainError.ErrorType.NotFound)]
    [InlineData(DomainError.ErrorType.Conflict)]
    [InlineData(DomainError.ErrorType.Unauthorized)]
    [InlineData(DomainError.ErrorType.Forbidden)]
    [InlineData(DomainError.ErrorType.Failure)]
    public void Constructor_ShouldSupportAllErrorTypes(DomainError.ErrorType errorType)
    {
        // Arrange & Act
        var error = new DomainError("Test.Code", "Test message", errorType);

        // Assert
        Assert.Equal(errorType, error.Type);
    }

    #endregion

    #region Equality Tests (ValueObject Behavior)

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);

        // Act & Assert
        Assert.True(error1.Equals(error2));
        Assert.True(error1 == error2);
        Assert.False(error1 != error2);
        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Equals_WithDifferentCode_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.Invalid", "User was not found.", DomainError.ErrorType.NotFound);

        // Act & Assert
        Assert.False(error1.Equals(error2));
        Assert.False(error1 == error2);
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equals_WithDifferentMessage_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.NotFound", "Different message.", DomainError.ErrorType.NotFound);

        // Act & Assert
        Assert.False(error1.Equals(error2));
        Assert.False(error1 == error2);
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new DomainError("User.Error", "Error message.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.Error", "Error message.", DomainError.ErrorType.Validation);

        // Act & Assert
        Assert.False(error1.Equals(error2));
        Assert.False(error1 == error2);
        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var error = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);

        // Act & Assert
        Assert.False(error.Equals(null));
        Assert.False(error == null);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);

        // Act & Assert
        Assert.Equal(error1.GetHashCode(), error2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHash()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.Invalid", "Invalid user.", DomainError.ErrorType.Validation);

        // Act & Assert
        Assert.NotEqual(error1.GetHashCode(), error2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var error = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Contains("User.NotFound", result);
        Assert.Contains("User was not found.", result);
        Assert.Contains("NotFound", result);
    }

    [Fact]
    public void ToString_ShouldIncludeAllProperties()
    {
        // Arrange
        var error = new DomainError("Test.Code", "Test message", DomainError.ErrorType.Validation);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Contains("Code", result);
        Assert.Contains("Message", result);
        Assert.Contains("Type", result);
    }

    #endregion

    #region ErrorType Enum Tests

    [Fact]
    public void ErrorType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)DomainError.ErrorType.Validation);
        Assert.Equal(1, (int)DomainError.ErrorType.NotFound);
        Assert.Equal(2, (int)DomainError.ErrorType.Conflict);
        Assert.Equal(3, (int)DomainError.ErrorType.Unauthorized);
        Assert.Equal(4, (int)DomainError.ErrorType.Forbidden);
        Assert.Equal(5, (int)DomainError.ErrorType.Failure);
    }

    [Fact]
    public void ErrorType_ShouldHaveSixValues()
    {
        // Arrange
        var values = Enum.GetValues<DomainError.ErrorType>();

        // Assert
        Assert.Equal(6, values.Length);
    }

    #endregion

    #region Dictionary/HashSet Usage Tests

    [Fact]
    public void DomainError_ShouldWorkCorrectlyInHashSet()
    {
        // Arrange
        var error1 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error2 = new DomainError("User.NotFound", "User was not found.", DomainError.ErrorType.NotFound);
        var error3 = new DomainError("User.Invalid", "Invalid user.", DomainError.ErrorType.Validation);

        HashSet<DomainError> hashSet = [];

        // Act
        hashSet.Add(error1);
        var added2 = hashSet.Add(error2);
        var added3 = hashSet.Add(error3);

        // Assert
        Assert.False(added2); // Duplicate not added
        Assert.True(added3); // Different error added
        Assert.Equal(2, hashSet.Count);
    }

    #endregion
}