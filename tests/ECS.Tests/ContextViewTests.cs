using Shouldly;

namespace ECS.Tests;

/// <summary>
/// Tests for Context.View<T>() method to ensure correct behavior and performance characteristics.
/// </summary>
public class ContextViewTests : IDisposable
{
    private readonly IContext _context;

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
        // Create a new context for each test
        _context = new Context();
    }

    public void Dispose()
    {
        // Clean up after each test
        _context.Clear();
    }

    [Fact]
    public void View_WithNoEntities_ReturnsEmptyResult()
    {
        // Act
        var view = _context.View<TestComponentA>();

        // Assert
        IsEmpty(view).ShouldBeTrue();
    }

    [Fact]
    public void View_WithEntitiesButNoMatchingComponents_ReturnsEmptyResult()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        entity1.AddComponent<TestComponentB>();
        entity2.AddComponent<TestComponentC>();
        _context.Register(entity1);
        _context.Register(entity2);
        
        // Act
        var view = _context.View<TestComponentA>();
        
        // Assert
        IsEmpty(view).ShouldBeTrue();
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
        
        _context.Register(entity1);
        _context.Register(entity2);
        _context.Register(entity3);
        
        // Act
        var view = _context.View<TestComponentA>();
        var results = Materialize(view);
        
        // Assert
        results.Count.ShouldBe(2);
        
        // Verify entity1 and component are returned
        var result1 = results.FirstOrDefault(r => r.Entity.Id == 1);
        result1.Entity.Id.ShouldBe(entity1.Id);
        result1.Component.Value.ShouldBe(10);
        
        // Verify entity2 and component are returned
        var result2 = results.FirstOrDefault(r => r.Entity.Id == 2);
        result2.Entity.Id.ShouldBe(entity2.Id);
        result2.Component.Value.ShouldBe(20);
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
        
        _context.Register(entity);
        
        // Act - Query for ComponentA
        var viewA = _context.View<TestComponentA>();
        var resultsA = Materialize(viewA);
        
        // Assert - Should get ComponentA
        resultsA.Count.ShouldBe(1);
        resultsA[0].Entity.Id.ShouldBe(entity.Id);
        resultsA[0].Component.Value.ShouldBe(42);
        
        // Act - Query for ComponentB
        var viewB = _context.View<TestComponentB>();
        var resultsB = Materialize(viewB);
        
        // Assert - Should get ComponentB
        resultsB.Count.ShouldBe(1);
        resultsB[0].Entity.Id.ShouldBe(entity.Id);
        resultsB[0].Component.Data.ShouldBe("test");
    }

    [Fact]
    public void View_ReturnsComponentReferencesNotCopies()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var component = entity.AddComponent<TestComponentA>();
        component.Value = 100;
        _context.Register(entity);
        
        // Act
        var view = _context.View<TestComponentA>();
        var result = Materialize(view)[0];
        
        // Modify through view result
        result.Component.Value = 200;
        
        // Assert - Changes should be reflected in original component
        component.Value.ShouldBe(200);
    }

    [Fact]
    public void View_CanIterateMultipleTimes()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        entity1.AddComponent<TestComponentA>();
        entity2.AddComponent<TestComponentA>();
        _context.Register(entity1);
        _context.Register(entity2);
        
        // Act
        var view = _context.View<TestComponentA>();
        var firstPass = Materialize(view);
        var secondPass = Materialize(view);
        
        // Assert - Both passes should return same results
        firstPass.Count.ShouldBe(2);
        secondPass.Count.ShouldBe(2);
        firstPass.Select(r => r.Entity.Id).OrderBy(id => id).ShouldBe(
                     secondPass.Select(r => r.Entity.Id).OrderBy(id => id));
    }

    [Fact]
    public void View_WithLargeNumberOfEntities_ReturnsAllMatching()
    {
        // Arrange
        const int entityCount = 1000;
        
        for (var i = 0; i < entityCount; i++)
        {
            var entity = Entity.Create(i, $"Entity{i}");
            var component = entity.AddComponent<TestComponentA>();
            component.Value = i;
            _context.Register(entity);
        }
        
        // Act
        var view = _context.View<TestComponentA>();
        var results = Materialize(view);
        
        // Assert
        results.Count.ShouldBe(entityCount);
        
        // Verify all entities are present and have correct values
        for (var i = 0; i < entityCount; i++)
        {
            var result = results.FirstOrDefault(r => r.Entity.Id == i);
            result.Component.Value.ShouldBe(i);
        }
    }

    [Fact]
    public void View_DeconstructionSyntax_Works()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        var component = entity.AddComponent<TestComponentA>();
        component.Value = 42;
        _context.Register(entity);
        
        // Act & Assert - Should be able to use deconstruction
        var view = _context.View<TestComponentA>();
        foreach (var (e, c) in view)
        {
            e.Id.ShouldBe(entity.Id);
            c.Value.ShouldBe(42);
        }
    }

    [Fact]
    public void View_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var entity = Entity.Create(1, "Entity");
        entity.AddComponent<TestComponentA>();
        _context.Register(entity);
        
        // Act - Call View multiple times
        var view1 = _context.View<TestComponentA>();
        var view2 = _context.View<TestComponentA>();
        var view3 = _context.View<TestComponentA>();
        
        var results1 = Materialize(view1);
        var results2 = Materialize(view2);
        var results3 = Materialize(view3);
        
        // Assert - All calls should return same data
        results1.Count.ShouldBe(1);
        results2.Count.ShouldBe(1);
        results3.Count.ShouldBe(1);
        results1[0].Entity.Id.ShouldBe(results2[0].Entity.Id);
        results2[0].Entity.Id.ShouldBe(results3[0].Entity.Id);
    }

    [Fact]
    public void View_AfterComponentRemoved_ExcludesEntity()
    {
        var entity = Entity.Create(1, "Entity");
        entity.AddComponent<TestComponentA>();
        _context.Register(entity);

        entity.RemoveComponent<TestComponentA>();

        IsEmpty(_context.View<TestComponentA>()).ShouldBeTrue();
    }

    [Fact]
    public void View_AfterComponentAddedAfterRegister_IncludesEntity()
    {
        var entity = Entity.Create(1, "Entity");
        _context.Register(entity);
        entity.AddComponent<TestComponentA>().Value = 7;

        var results = Materialize(_context.View<TestComponentA>());

        results.Count.ShouldBe(1);
        results[0].Component.Value.ShouldBe(7);
    }

    [Fact]
    public void View_TwoComponents_ReturnsOnlyEntitiesWithBoth()
    {
        var withBoth = Entity.Create(1, "Both");
        withBoth.AddComponent<TestComponentA>().Value = 1;
        withBoth.AddComponent<TestComponentB>().Data = "x";

        var onlyA = Entity.Create(2, "OnlyA");
        onlyA.AddComponent<TestComponentA>().Value = 2;

        _context.Register(withBoth);
        _context.Register(onlyA);

        var results = Materialize(_context.View<TestComponentA, TestComponentB>());

        results.Count.ShouldBe(1);
        results[0].Entity.Id.ShouldBe(1);
        results[0].Component1.Value.ShouldBe(1);
        results[0].Component2.Data.ShouldBe("x");
    }

    [Fact]
    public void View_TwoComponents_SkipsEntityWhenComponentRemovedDuringIteration()
    {
        var entity = Entity.Create(1, "Both");
        entity.AddComponent<TestComponentA>().Value = 1;
        entity.AddComponent<TestComponentB>().Data = "x";
        _context.Register(entity);

        entity.RemoveComponent<TestComponentB>();

        var results = Materialize(_context.View<TestComponentA, TestComponentB>());

        results.ShouldBeEmpty();
    }

    [Fact]
    public void View_ThreeComponents_ReturnsOnlyEntitiesWithAll()
    {
        var complete = Entity.Create(1, "All");
        complete.AddComponent<TestComponentA>();
        complete.AddComponent<TestComponentB>();
        complete.AddComponent<TestComponentC>().Flag = true;

        var partial = Entity.Create(2, "Partial");
        partial.AddComponent<TestComponentA>();
        partial.AddComponent<TestComponentB>();

        _context.Register(complete);
        _context.Register(partial);

        var results = new List<(Entity Entity, TestComponentA Component1, TestComponentB Component2)>();
        foreach (var tuple in _context.View<TestComponentA, TestComponentB>())
        {
            if (tuple.Entity.HasComponent<TestComponentC>())
                results.Add(tuple);
        }

        results.Count.ShouldBe(1);
        results[0].Entity.Id.ShouldBe(1);
        results[0].Entity.GetComponent<TestComponentC>().Flag.ShouldBeTrue();
    }

    [Fact]
    public void View_SparseComponentType_DoesNotScanUnrelatedEntities()
    {
        const int total = 500;
        const int withA = 5;

        for (var i = 0; i < total; i++)
        {
            var entity = Entity.Create(i, $"Entity{i}");
            if (i < withA)
                entity.AddComponent<TestComponentA>().Value = i;
            else
                entity.AddComponent<TestComponentB>().Data = "b";

            _context.Register(entity);
        }

        Count(_context.View<TestComponentA>()).ShouldBe(withA);
    }

    [Fact]
    public void Entities_WithNoEntities_ReturnsEmpty()
    {
        _context.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void Entities_PreservesRegistrationOrder()
    {
        var entity1 = Entity.Create(1, "First");
        var entity2 = Entity.Create(2, "Second");
        var entity3 = Entity.Create(3, "Third");
        _context.Register(entity1);
        _context.Register(entity2);
        _context.Register(entity3);

        var ids = _context.Entities.Select(e => e.Id).ToList();

        ids.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Entities_AfterRemove_ExcludesRemovedEntity()
    {
        var entity1 = Entity.Create(1, "Keep");
        var entity2 = Entity.Create(2, "Remove");
        _context.Register(entity1);
        _context.Register(entity2);

        _context.Remove(entity2.Id);

        var ids = _context.Entities.Select(e => e.Id).ToList();
        ids.ShouldBe([1]);
    }

    [Fact]
    public void Entities_AfterClear_ReturnsEmpty()
    {
        _context.Register(Entity.Create(1, "Entity"));
        _context.Clear();

        _context.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void Contains_ReturnsTrueForRegisteredEntity()
    {
        var entity = Entity.Create(42, "Entity");
        _context.Register(entity);

        _context.Contains(42).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalseForUnregisteredEntity()
    {
        _context.Contains(99).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalseAfterRemove()
    {
        var entity = Entity.Create(1, "Entity");
        _context.Register(entity);
        _context.Remove(entity.Id);

        _context.Contains(entity.Id).ShouldBeFalse();
    }

    private static bool IsEmpty<TComponent>(ComponentView<TComponent> view)
        where TComponent : IComponent
    {
        foreach (var _ in view)
            return false;
        return true;
    }

    private static int Count<TComponent>(ComponentView<TComponent> view)
        where TComponent : IComponent
    {
        var n = 0;
        foreach (var _ in view)
            n++;
        return n;
    }

    private static List<(Entity Entity, TComponent Component)> Materialize<TComponent>(ComponentView<TComponent> view)
        where TComponent : IComponent
    {
        var results = new List<(Entity, TComponent)>();
        foreach (var item in view)
            results.Add(item);
        return results;
    }

    private static List<(Entity Entity, T1 Component1, T2 Component2)> Materialize<T1, T2>(DualComponentView<T1, T2> view)
        where T1 : IComponent
        where T2 : IComponent
    {
        var results = new List<(Entity, T1, T2)>();
        foreach (var item in view)
            results.Add(item);
        return results;
    }
}
