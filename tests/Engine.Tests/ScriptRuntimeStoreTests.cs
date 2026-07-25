using ECS;
using Engine.Scene;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class ScriptRuntimeStoreTests
{
    [Fact]
    public void TryGet_ReturnsFalse_WhenEmpty()
    {
        var store = new ScriptRuntimeStore();

        store.TryGet(1, out _).ShouldBeFalse();
    }

    [Fact]
    public void Set_AndTryGet_ReturnsScript()
    {
        var store = new ScriptRuntimeStore();
        var script = new StubScript();

        store.Set(42, script);

        store.TryGet(42, out var found).ShouldBeTrue();
        found.ShouldBeSameAs(script);
    }

    [Fact]
    public void Remove_RemovesEntry()
    {
        var store = new ScriptRuntimeStore();
        store.Set(1, new StubScript());

        store.Remove(1);

        store.TryGet(1, out _).ShouldBeFalse();
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var store = new ScriptRuntimeStore();
        store.Set(1, new StubScript());
        store.Set(2, new StubScript());

        store.Clear();

        store.TryGet(1, out _).ShouldBeFalse();
        store.TryGet(2, out _).ShouldBeFalse();
    }

    private sealed class StubScript : ScriptableEntity
    {
        public StubScript() : base(new ComponentAccessor(), null!, null!, Substitute.For<IPhysicsQueries>(), NullEntityHierarchy.Instance) { }
    }
}
