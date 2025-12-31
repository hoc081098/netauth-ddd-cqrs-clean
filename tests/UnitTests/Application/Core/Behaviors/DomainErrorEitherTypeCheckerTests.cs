using LanguageExt;
using NetAuth.Application.Core.Behaviors;
using NetAuth.Domain.Core.Primitives;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace NetAuth.UnitTests.Application.Core.Behaviors;

public class DomainErrorEitherTypeCheckerTests
{
    [Fact]
    public void IsDomainErrorEither_WithEitherDomainErrorString_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<DomainError, string>>(out var rightType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(string), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithEitherDomainErrorInt_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<DomainError, int>>(out var rightType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(int), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithEitherDomainErrorCustomType_ShouldReturnTrue()
    {
        // Arrange & Act
        var result =
            DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<DomainError, TestResult>>(out var rightType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(TestResult), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithEitherStringInt_ShouldReturnFalse()
    {
        // Arrange & Act - Left type is not DomainError
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<string, int>>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithNonGenericType_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<string>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithInt_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<int>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithList_ShouldReturnFalse()
    {
        // Arrange & Act - Generic but not Either
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<List<string>>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithDictionary_ShouldReturnFalse()
    {
        // Arrange & Act - Generic with two type params but not Either
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Dictionary<string, int>>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithTask_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Task<string>>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithEitherExceptionInt_ShouldReturnFalse()
    {
        // Arrange & Act - Left type is Exception, not DomainError
        var result = DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<Exception, int>>(out var rightType);

        // Assert
        Assert.False(result);
        Assert.Equal(typeof(void), rightType);
    }

    [Fact]
    public void IsDomainErrorEither_WithUnit_ShouldReturnTrue()
    {
        // Arrange & Act
        var result =
            DomainErrorEitherTypeChecker.IsDomainErrorEither<Either<DomainError, MediatR.Unit>>(out var rightType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(MediatR.Unit), rightType);
    }

    #region Test Types

    private sealed record TestResult(string Value);

    #endregion
}