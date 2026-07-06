using Shouldly;

namespace ECS.Tests;

public class EntityEqualityTests
{
    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(1, "Entity2");

        // Act & Assert
        entity1.Equals(entity2).ShouldBeTrue();
        entity1.Equals((object)entity2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity1");

        // Act & Assert
        entity1.Equals(entity2).ShouldBeFalse();
        entity1.Equals((object)entity2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");

        // Act & Assert
        entity.Equals((Entity?)null).ShouldBeFalse();
        entity.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithWrongType_ReturnsFalse()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var notAnEntity = "not an entity";

        // Act & Assert
        entity.Equals(notAnEntity).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_RemainsStableAfterAddingComponents()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var hashBeforeAdd = entity.GetHashCode();

        // Act - Add a component
        entity.AddComponent<TestComponent>(new TestComponent());
        var hashAfterAdd = entity.GetHashCode();

        // Assert - Hash should remain the same
        hashAfterAdd.ShouldBe(hashBeforeAdd);
    }

    [Fact]
    public void GetHashCode_RemainsStableAfterRemovingComponents()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        entity.AddComponent<TestComponent>();
        var hashBeforeRemove = entity.GetHashCode();

        // Act - Remove component
        entity.RemoveComponent<TestComponent>();
        var hashAfterRemove = entity.GetHashCode();

        // Assert - Hash should remain the same
        hashAfterRemove.ShouldBe(hashBeforeRemove);
    }

    [Fact]
    public void GetHashCode_RemainsStableAfterChangingName()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var hashBeforeNameChange = entity.GetHashCode();

        // Act - Change name
        entity.Name = "NewName";
        var hashAfterNameChange = entity.GetHashCode();

        // Assert - Hash should remain the same
        hashAfterNameChange.ShouldBe(hashBeforeNameChange);
    }

    [Fact]
    public void GetHashCode_DifferentForDifferentIds()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity");
        var entity2 = Entity.Create(2, "Entity");

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert - Different entities should (likely) have different hashes
        hash2.ShouldNotBe(hash1);
    }

    [Fact]
    public void HashSet_ContainsEntity_AfterAddingComponents()
    {
        // Arrange
        var entitySet = new HashSet<Entity>();
        var entity = Entity.Create(1, "Entity");
        entitySet.Add(entity);

        // Act - Modify entity by adding component
        entity.AddComponent<TestComponent>();

        // Assert - Entity should still be found in HashSet
        entitySet.ShouldContain(entity);
    }

    [Fact]
    public void Dictionary_CanRetrieveEntity_AfterAddingComponents()
    {
        // Arrange
        var entityDict = new Dictionary<Entity, string>();
        var entity = Entity.Create(1, "Entity");
        entityDict[entity] = "test value";

        // Act - Modify entity by adding component
        entity.AddComponent<TestComponent>();

        // Assert - Entity should still be a valid key
        entityDict.ContainsKey(entity).ShouldBeTrue();
        entityDict[entity].ShouldBe("test value");
    }

    [Fact]
    public void EqualsOperator_UsesOverriddenEquals()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(1, "Entity2");

        // Act & Assert - Equals method should be used for comparisons
        entity1.Equals(entity2).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithSameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(1, "Entity2");

        // Act & Assert
        (entity1 == entity2).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity1");

        // Act & Assert
        (entity1 == entity2).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ReturnsTrue()
    {
        // Arrange
        Entity? entity1 = null;
        Entity? entity2 = null;

        // Act & Assert
        (entity1 == entity2).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithOneNull_ReturnsFalse()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        Entity? nullEntity = null;

        // Act & Assert
        (entity == nullEntity).ShouldBeFalse();
        (nullEntity == entity).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_WithSameReference_ReturnsTrue()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");

        // Act & Assert
        (entity == entity).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameId_ReturnsFalse()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(1, "Entity2");

        // Act & Assert
        (entity1 != entity2).ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentId_ReturnsTrue()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity1");

        // Act & Assert
        (entity1 != entity2).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_WithBothNull_ReturnsFalse()
    {
        // Arrange
        Entity? entity1 = null;
        Entity? entity2 = null;

        // Act & Assert
        (entity1 != entity2).ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_WithOneNull_ReturnsTrue()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        Entity? nullEntity = null;

        // Act & Assert
        (entity != nullEntity).ShouldBeTrue();
        (nullEntity != entity).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_ConsistentWithEqualsMethod()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(1, "Entity2");
        var entity3 = Entity.Create(2, "Entity3");

        // Act & Assert - == should match Equals()
        (entity1 == entity2).ShouldBe(entity1.Equals(entity2));
        (entity1 == entity3).ShouldBe(entity1.Equals(entity3));
    }

    // Test component for validation
    private class TestComponent : IComponent
    {
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }
}
