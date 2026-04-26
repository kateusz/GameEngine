using System.Numerics;
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

    [Fact]
    public void GetWorldTransform_NoParent_EqualsLocal()
    {
        var t = new TransformComponent(new Vector3(1, 2, 3), Vector3.Zero, Vector3.One);

        var world = t.GetWorldTransform(_ => null);

        world.ShouldBe(t.GetTransform());
    }

    [Fact]
    public void GetWorldTransform_WithParent_IsParentWorldTimesLocal()
    {
        var parent = new TransformComponent(new Vector3(10, 0, 0), Vector3.Zero, Vector3.One);
        var child = new TransformComponent(new Vector3(1, 0, 0), Vector3.Zero, Vector3.One);
        child.SetParentIdInternal(1);

        var world = child.GetWorldTransform(id => id == 1 ? parent : null);

        var expected = child.GetTransform() * parent.GetWorldTransform(_ => null);
        world.ShouldBe(expected);
    }

    [Fact]
    public void GetWorldTransform_CachesResult()
    {
        var t = new TransformComponent(new Vector3(1, 0, 0), Vector3.Zero, Vector3.One);
        var calls = 0;

        var w1 = t.GetWorldTransform(_ => { calls++; return null; });
        var w2 = t.GetWorldTransform(_ => { calls++; return null; });

        w1.ShouldBe(w2);
        calls.ShouldBe(0);
    }

    [Fact]
    public void MutatingTranslation_MarksWorldDirty()
    {
        var t = new TransformComponent();
        _ = t.GetWorldTransform(_ => null);
        t.Translation = new Vector3(5, 0, 0);

        t.IsWorldDirty.ShouldBeTrue();
    }

    [Fact]
    public void GetWorldTransform_AfterMutation_ReturnsRecomputedValue()
    {
        var t = new TransformComponent(new Vector3(1, 0, 0), Vector3.Zero, Vector3.One);
        var first = t.GetWorldTransform(_ => null);

        t.Translation = new Vector3(99, 0, 0);
        var second = t.GetWorldTransform(_ => null);

        second.ShouldNotBe(first);
        second.ShouldBe(t.GetTransform());
    }
}
