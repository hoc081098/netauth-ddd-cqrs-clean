using System.Diagnostics.CodeAnalysis;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.UnitTests.Domain.Core.Primitives;

[SuppressMessage("Assertions", "xUnit2024:Do not use boolean asserts for simple equality tests")]
[SuppressMessage("ReSharper", "EqualExpressionComparison")]
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
[SuppressMessage("ReSharper", "VariableCanBeNotNullable")]
[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
[SuppressMessage("ReSharper", "AppendToCollectionExpression")]
public class ValueObjectTests
{
    #region Test Value Objects

    private sealed class Address(string street, string city, string zipCode) : ValueObject
    {
        public string Street { get; } = street;
        public string City { get; } = city;
        public string ZipCode { get; } = zipCode;

        protected override IEnumerable<object> GetAtomicValues() => [Street, City, ZipCode];
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object> GetAtomicValues() => [Amount, Currency];
    }

    private sealed class SingleValueObject(int value) : ValueObject
    {
        public int Value { get; } = value;

        protected override IEnumerable<object> GetAtomicValues() => [Value];
    }

    #endregion

    #region Equals Tests

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.True(address1.Equals(address2));
        Assert.True(address1 == address2);
        Assert.False(address1 != address2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("456 Oak Ave", "Los Angeles", "90001");

        // Act & Assert
        Assert.False(address1.Equals(address2));
        Assert.False(address1 == address2);
        Assert.True(address1 != address2);
    }

    [Fact]
    public void Equals_WithPartiallyDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10002"); // Different ZipCode

        // Act & Assert
        Assert.False(address1.Equals(address2));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.False(address.Equals(null));
        Assert.False(address == null);
        Assert.False(null == address);
        Assert.True(address != null);
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.True(address.Equals(address));
        Assert.True(ReferenceEquals(address, address));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "10001");
        var money = new Money(100m, "USD");

        // Act & Assert
        Assert.False(address.Equals(money));
        Assert.False(address == money);
        Assert.True(address != money);
    }

    [Fact]
    public void Equals_WithObjectParameter_ShouldWorkCorrectly()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        object? address2 = new Address("123 Main St", "New York", "10001");
        object? differentAddress = new Address("456 Oak Ave", "Los Angeles", "90001");

        // Act & Assert
        Assert.True(address1.Equals(address2));
        Assert.False(address1.Equals(differentAddress));
        Assert.False(address1.Equals("not an address"));
    }

    [Fact]
    public void Equals_BothNull_ShouldReturnTrue()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;

        // Act & Assert
        Assert.True(address1 == address2);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHash()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
        Assert.True(address1.GetHashCode() == address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHash()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("456 Oak Ave", "Los Angeles", "90001");

        // Act & Assert
        Assert.NotEqual(address1.GetHashCode(), address2.GetHashCode());
        Assert.True(address1.GetHashCode() != address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "10001");

        // Act
        var hash1 = address.GetHashCode();
        var hash2 = address.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.True(hash1 == hash2);
    }

    [Fact]
    public void GetHashCode_ShouldWorkWithSingleValue()
    {
        // Arrange
        var value1 = new SingleValueObject(42);
        var value2 = new SingleValueObject(42);
        var value3 = new SingleValueObject(43);

        // Act & Assert
        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value3.GetHashCode());
    }

    #endregion

    #region Dictionary/HashSet Usage Tests

    [Fact]
    public void ValueObject_ShouldWorkCorrectlyInHashSet()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");
        var address3 = new Address("456 Oak Ave", "Los Angeles", "90001");

        HashSet<Address> hashSet = [];

        // Act
        hashSet.Add(address1);
        var added2 = hashSet.Add(address2);
        var added3 = hashSet.Add(address3);

        // Assert
        Assert.False(added2); // Duplicate not added
        Assert.True(added3); // Different address added
        Assert.Equal(2, hashSet.Count);
    }

    [Fact]
    public void ValueObject_ShouldWorkCorrectlyAsDictionaryKey()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        var dictionary = new Dictionary<Address, string>
        {
            [address1] = "Home",
        };

        // Act & Assert
        Assert.True(dictionary.ContainsKey(address2));
        Assert.Equal("Home", dictionary[address1]);
        Assert.Equal("Home", dictionary[address2]);
    }

    #endregion
}