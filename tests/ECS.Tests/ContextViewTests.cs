namespace ECS.Tests;

/// <summary>
/// Tests for Context.View<T>() method to ensure correct behavior and performance characteristics.
/// </summary>
public class ContextViewTests : IDisposable
{
    private class TestComponentA : IComponent { public int Value { get; set; }
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }
    private class TestComponentB : IComponent { public string Data { get; set; } = "";
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }
    private class TestComponentC : IComponent { public bool Flag { get; set; }
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }

    public ContextViewTests()
    {
        // Clear entities before each test
        Context.Instance.Clear();
    }

    public void Dispose()
    {
        // Clean up after each test
        Context.Instance.Clear();
    }

    [Fact]
    public void View_WithNoEntities_ReturnsEmptyResult()
    {
        // Act
        var view = Context.Instance.View<TestComponentA>();
        
        // Assert
        Assert.Empty(view);
    }

    [Fact]
    public void View_WithEntitiesButNoMatchingComponents_ReturnsEmptyResult()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        entity1.AddComponent<TestComponentB>();
        entity2.AddComponent<TestComponentC>();
        Context.Instance.Register(entity1);
        Context.Instance.Register(entity2);
        
        // Act
        var view = Context.Instance.View<TestComponentA>();
        
        // Assert
        Assert.Empty(view);
    }

    [Fact]
    public void View_WithMatchingComponents_ReturnsCorrectEntitiesAndComponents()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        var entity3 = Entity.Create(3, "Entity3");
        
        var componentA1 = entity1.AddComponent<TestComponentA>();
        componentA1.Value = 10;
        
        var componentA2 = entity2.AddComponent<TestComponentA>();
        componentA2.Value = 20;
        
        entity3.AddComponent<TestComponentB>(); // Different component
        
        Context.Instance.Register(entity1);
        Context.Instance.Register(entity2);
        Context.Instance.Register(entity3);
        
        // Act
        var view = Context.Instance.View<TestComponentA>();
        var results = view.ToList();
        
        // Assert
        Assert.Equal(2, results.Count);
        
        // Verify entity1 and component are returned
        var result1 = results.FirstOrDefault(r => r.Entity.Id == 1);
        Assert.Equal(entity1.Id, result1.Entity.Id);
        Assert.Equal(10, result1.Component.Value);
        
        // Verify entity2 and component are returned
        var result2 = results.FirstOrDefault(r => r.Entity.Id == 2);
        Assert.Equal(entity2.Id, result2.Entity.Id);
        Assert.Equal(20, result2.Component.Value);
    }

    [Fact]
    public void View_WithMultipleComponents_OnlyReturnsSpecificComponentType()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        
        var componentA = entity.AddComponent<TestComponentA>();
        componentA.Value = 42;
        
        var componentB = entity.AddComponent<TestComponentB>();
        componentB.Data = "test";
        
        Context.Instance.Register(entity);
        
        // Act - Query for ComponentA
        var viewA = Context.Instance.View<TestComponentA>();
        var resultsA = viewA.ToList();
        
        // Assert - Should get ComponentA
        Assert.Single(resultsA);
        Assert.Equal(entity.Id, resultsA[0].Entity.Id);
        Assert.Equal(42, resultsA[0].Component.Value);
        
        // Act - Query for ComponentB
        var viewB = Context.Instance.View<TestComponentB>();
        var resultsB = viewB.ToList();
        
        // Assert - Should get ComponentB
        Assert.Single(resultsB);
        Assert.Equal(entity.Id, resultsB[0].Entity.Id);
        Assert.Equal("test", resultsB[0].Component.Data);
    }

    [Fact]
    public void View_ReturnsComponentReferencesNotCopies()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var component = entity.AddComponent<TestComponentA>();
        component.Value = 100;
        Context.Instance.Register(entity);
        
        // Act
        var view = Context.Instance.View<TestComponentA>();
        var result = view.First();
        
        // Modify through view result
        result.Component.Value = 200;
        
        // Assert - Changes should be reflected in original component
        Assert.Equal(200, component.Value);
    }

    [Fact]
    public void View_CanIterateMultipleTimes()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        entity1.AddComponent<TestComponentA>();
        entity2.AddComponent<TestComponentA>();
        Context.Instance.Register(entity1);
        Context.Instance.Register(entity2);
        
        // Act
        var view = Context.Instance.View<TestComponentA>();
        var firstPass = view.ToList();
        var secondPass = view.ToList();
        
        // Assert - Both passes should return same results
        Assert.Equal(2, firstPass.Count);
        Assert.Equal(2, secondPass.Count);
        Assert.Equal(firstPass.Select(r => r.Entity.Id).OrderBy(id => id), 
                     secondPass.Select(r => r.Entity.Id).OrderBy(id => id));
    }

    [Fact]
    public void View_WithLargeNumberOfEntities_ReturnsAllMatching()
    {
        // Arrange
        const int entityCount = 1000;
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = Entity.Create(i, $"Entity{i}");
            var component = entity.AddComponent<TestComponentA>();
            component.Value = i;
            Context.Instance.Register(entity);
        }
        
        // Act
        var view = Context.Instance.View<TestComponentA>();
        var results = view.ToList();
        
        // Assert
        Assert.Equal(entityCount, results.Count);
        
        // Verify all entities are present and have correct values
        for (int i = 0; i < entityCount; i++)
        {
            var result = results.FirstOrDefault(r => r.Entity.Id == i);
            Assert.Equal(i, result.Component.Value);
        }
    }

    [Fact]
    public void View_DeconstructionSyntax_Works()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var component = entity.AddComponent<TestComponentA>();
        component.Value = 42;
        Context.Instance.Register(entity);
        
        // Act & Assert - Should be able to use deconstruction
        var view = Context.Instance.View<TestComponentA>();
        foreach (var (e, c) in view)
        {
            Assert.Equal(entity.Id, e.Id);
            Assert.Equal(42, c.Value);
        }
    }

    [Fact]
    public void View_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        entity.AddComponent<TestComponentA>();
        Context.Instance.Register(entity);
        
        // Act - Call View multiple times
        var view1 = Context.Instance.View<TestComponentA>();
        var view2 = Context.Instance.View<TestComponentA>();
        var view3 = Context.Instance.View<TestComponentA>();
        
        var results1 = view1.ToList();
        var results2 = view2.ToList();
        var results3 = view3.ToList();
        
        // Assert - All calls should return same data
        Assert.Single(results1);
        Assert.Single(results2);
        Assert.Single(results3);
        Assert.Equal(results1[0].Entity.Id, results2[0].Entity.Id);
        Assert.Equal(results2[0].Entity.Id, results3[0].Entity.Id);
    }
}
