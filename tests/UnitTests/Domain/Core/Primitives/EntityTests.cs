using System.Diagnostics.CodeAnalysis;
using NetAuth.Domain.Core.Primitives;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable EqualExpressionComparison

namespace NetAuth.UnitTests.Domain.Core.Primitives;

[SuppressMessage("Assertions", "xUnit2024:Do not use boolean asserts for simple equality tests")]
[SuppressMessage("ReSharper", "EqualExpressionComparison")]
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
[SuppressMessage("ReSharper", "VariableCanBeNotNullable")]
[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
[SuppressMessage("ReSharper", "AppendToCollectionExpression")]
[SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code")]
public class EntityTests
{
    #region Test Entities

    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; }

        public TestEntity(Guid id, string name) : base(id)
        {
            Name = name;
        }

        // For testing transient entity
        public TestEntity()
        {
            Name = string.Empty;
        }
    }

    private sealed class IntEntity(int id) : Entity<int>(id);

    private sealed class StringEntity(string id) : Entity<string>(id);

    #endregion

    #region Equals Tests

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2"); // Different name, same `Id`.

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
        Assert.True(entity1 == entity2);
        Assert.False(entity1 != entity2);
        Assert.Equal(entity1, entity2);
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Entity 1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Entity 1"); // Same name, different `Id`.

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
        Assert.False(entity1 == entity2);
        Assert.True(entity1 != entity2);
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Entity");

        // Act & Assert
        Assert.False(entity.Equals(null));
        Assert.False(entity == null);
        Assert.False(null == entity);
        Assert.True(entity != null);
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Entity");

        // Act & Assert
        Assert.True(entity.Equals(entity));
        Assert.True(ReferenceEquals(entity, entity));
        Assert.Equal(entity, entity);
    }

    [Fact]
    public void Equals_BothTransientEntities_ShouldReturnFalse()
    {
        // Arrange - Transient entities have empty Guid
        var entity1 = new TestEntity(Guid.Empty, "Entity 1");
        var entity2 = new TestEntity(Guid.Empty, "Entity 2");

        // Act & Assert - Transient entities should never be equal
        Assert.False(entity1.Equals(entity2));
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void Equals_OneTransientEntity_ShouldReturnFalse()
    {
        // Arrange
        var persistedEntity = new TestEntity(Guid.NewGuid(), "Persisted");
        var transientEntity = new TestEntity(Guid.Empty, "Transient");

        // Act & Assert
        Assert.False(persistedEntity.Equals(transientEntity));
        Assert.False(transientEntity.Equals(persistedEntity));
        Assert.NotEqual(persistedEntity, transientEntity);
    }

    [Fact]
    public void Equals_BothNull_ShouldReturnTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        Assert.True(entity1 == entity2);
        Assert.Equal(entity1, entity2);
    }

    [Fact]
    public void Equals_WithObjectParameter_ShouldWorkCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        object? entity2 = new TestEntity(id, "Entity 2");
        object? differentEntity = new TestEntity(Guid.NewGuid(), "Different");

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
        Assert.False(entity1.Equals(differentEntity));
        Assert.False(entity1.Equals("not an entity"));
        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, differentEntity);
        Assert.NotEqual(entity2, differentEntity);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHash()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2");

        // Act & Assert
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentId_ShouldReturnDifferentHash()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Entity");
        var entity2 = new TestEntity(Guid.NewGuid(), "Entity");

        // Act & Assert
        Assert.NotEqual(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Entity");

        // Act
        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    #endregion

    #region Different Id Types Tests

    [Fact]
    public void Entity_WithIntId_ShouldWorkCorrectly()
    {
        // Arrange
        var entity1 = new IntEntity(1);
        var entity2 = new IntEntity(1);
        var entity3 = new IntEntity(2);

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
        Assert.False(entity1.Equals(entity3));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_WithStringId_ShouldWorkCorrectly()
    {
        // Arrange
        var entity1 = new StringEntity("abc");
        var entity2 = new StringEntity("abc");
        var entity3 = new StringEntity("xyz");

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
        Assert.False(entity1.Equals(entity3));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    #endregion

    #region Id Property Tests

    [Fact]
    public void Id_ShouldBeAccessible()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id, "Entity");

        // Act & Assert
        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Id_WithDefaultConstructor_ShouldBeDefault()
    {
        // Arrange
        var entity = new TestEntity();

        // Act & Assert
        Assert.Equal(Guid.Empty, entity.Id);
    }

    #endregion

    #region Dictionary/HashSet Usage Tests

    [Fact]
    public void Entity_ShouldWorkCorrectlyInHashSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2"); // Same Id
        var entity3 = new TestEntity(Guid.NewGuid(), "Entity 3");

        var hashSet = new HashSet<TestEntity>();

        // Act
        hashSet.Add(entity1);
        var added2 = hashSet.Add(entity2);
        var added3 = hashSet.Add(entity3);

        // Assert
        Assert.False(added2); // Same `Id` not added
        Assert.True(added3); // Different Id added
        Assert.Equal(2, hashSet.Count);
    }

    [Fact]
    public void Entity_ShouldWorkCorrectlyAsDictionaryKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2");

        var dictionary = new Dictionary<TestEntity, string>
        {
            [entity1] = "Value"
        };

        // Act & Assert
        Assert.True(dictionary.ContainsKey(entity2));
        Assert.Equal("Value", dictionary[entity1]);
        Assert.Equal("Value", dictionary[entity2]);
    }

    #endregion
}