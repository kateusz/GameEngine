using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Components;

public class TransformComponentHierarchyTests
{
    [Fact]
    public void ParentId_NewComponent_IsNull()
    {
        var t = new TransformComponent();
        t.ParentId.ShouldBeNull();
    }

    [Fact]
    public void ChildIds_NewComponent_IsEmpty()
    {
        var t = new TransformComponent();
        t.ChildIds.ShouldBeEmpty();
    }

    [Fact]
    public void SetParentIdInternal_UpdatesParentId()
    {
        var t = new TransformComponent();
        t.SetParentIdInternal(42);
        t.ParentId.ShouldBe(42);

        t.SetParentIdInternal(null);
        t.ParentId.ShouldBeNull();
    }

    [Fact]
    public void AddChildIdInternal_AppendsToChildIds()
    {
        var t = new TransformComponent();
        t.AddChildIdInternal(7);
        t.AddChildIdInternal(8);
        t.ChildIds.ShouldBe(new[] { 7, 8 });
    }

    [Fact]
    public void RemoveChildIdInternal_RemovesEntry()
    {
        var t = new TransformComponent();
        t.AddChildIdInternal(7);
        t.AddChildIdInternal(8);
        t.RemoveChildIdInternal(7);
        t.ChildIds.ShouldBe(new[] { 8 });
    }
}
