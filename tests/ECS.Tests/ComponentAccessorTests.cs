using Shouldly;

namespace ECS.Tests;

public class ComponentAccessorTests
{
    private class TestComponent : IComponent
    {
        public int Value { get; set; }
        public IComponent Clone() => throw new NotImplementedException();
    }

    private class AnotherComponent : IComponent
    {
        public IComponent Clone() => throw new NotImplementedException();
    }

    [Fact]
    public void SetEntity_ThenGetComponent_ReturnsComponent()
    {
        var entity = Entity.Create(1, "Test");
        entity.AddComponent<TestComponent>().Value = 42;
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        var component = accessor.GetComponent<TestComponent>();

        component.Value.ShouldBe(42);
    }

    [Fact]
    public void HasComponent_WhenComponentExists_ReturnsTrue()
    {
        var entity = Entity.Create(1, "Test");
        entity.AddComponent<TestComponent>();
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        accessor.HasComponent<TestComponent>().ShouldBeTrue();
    }

    [Fact]
    public void HasComponent_WhenComponentDoesNotExist_ReturnsFalse()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        accessor.HasComponent<TestComponent>().ShouldBeFalse();
    }

    [Fact]
    public void AddComponent_Parameterless_AddsAndReturnsComponent()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        var component = accessor.AddComponent<TestComponent>();

        component.ShouldNotBeNull();
        entity.HasComponent<TestComponent>().ShouldBeTrue();
    }

    [Fact]
    public void AddComponent_WithInstance_AddsAndReturnsSameInstance()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);
        var component = new TestComponent { Value = 99 };

        accessor.AddComponent(component);

        entity.GetComponent<TestComponent>().Value.ShouldBe(99);
    }

    [Fact]
    public void RemoveComponent_RemovesComponent()
    {
        var entity = Entity.Create(1, "Test");
        entity.AddComponent<TestComponent>();
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        accessor.RemoveComponent<TestComponent>();

        entity.HasComponent<TestComponent>().ShouldBeFalse();
    }

    [Fact]
    public void OperationsReflectEntityChanges()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        accessor.AddComponent<TestComponent>();
        accessor.HasComponent<TestComponent>().ShouldBeTrue();

        accessor.RemoveComponent<TestComponent>();
        accessor.HasComponent<TestComponent>().ShouldBeFalse();
    }

    [Fact]
    public void GetComponent_ForNonExistentComponent_Throws()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        Should.Throw<InvalidOperationException>(accessor.GetComponent<TestComponent>);
    }

    [Fact]
    public void DifferentEntity_ReturnsDifferentComponents()
    {
        var entityA = Entity.Create(1, "A");
        entityA.AddComponent<TestComponent>().Value = 10;
        var entityB = Entity.Create(2, "B");
        entityB.AddComponent<TestComponent>().Value = 20;

        var accessor = new ComponentAccessor();
        accessor.SetEntity(entityA);
        accessor.GetComponent<TestComponent>().Value.ShouldBe(10);

        accessor.SetEntity(entityB);
        accessor.GetComponent<TestComponent>().Value.ShouldBe(20);
    }

    [Fact]
    public void AddComponent_Duplicate_Throws()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);
        accessor.AddComponent<TestComponent>();

        Should.Throw<InvalidOperationException>(accessor.AddComponent<TestComponent>);
    }

    [Fact]
    public void MultipleComponentTypes_WorkIndependently()
    {
        var entity = Entity.Create(1, "Test");
        var accessor = new ComponentAccessor();
        accessor.SetEntity(entity);

        accessor.AddComponent<TestComponent>();
        accessor.AddComponent<AnotherComponent>();

        accessor.HasComponent<TestComponent>().ShouldBeTrue();
        accessor.HasComponent<AnotherComponent>().ShouldBeTrue();

        accessor.RemoveComponent<TestComponent>();

        accessor.HasComponent<TestComponent>().ShouldBeFalse();
        accessor.HasComponent<AnotherComponent>().ShouldBeTrue();
    }
}
