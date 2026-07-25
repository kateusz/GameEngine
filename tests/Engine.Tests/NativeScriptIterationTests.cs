using Audio;
using ECS;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using Input;
using NSubstitute;
using SceneComponents;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class NativeScriptIterationTests
{
    [Fact]
    public void Shutdown_CallsOnDestroyAndRemovesFromStore()
    {
        var context = new Context();
        var store = new ScriptRuntimeStore();
        var entity = Entity.Create(1, "scripted");
        entity.AddComponent(new NativeScriptComponent { ScriptTypeName = "Test" });
        context.Register(entity);
        var script = new TrackingScript();
        store.Set(entity.Id, script);

        NativeScriptIteration.Shutdown(context, store);

        store.TryGet(entity.Id, out _).ShouldBeFalse();
        script.DestroyCalled.ShouldBeTrue();
    }

    [Fact]
    public void ProcessEvent_DoesNotCreateInstances()
    {
        var context = new Context();
        var store = new ScriptRuntimeStore();
        var entity = Entity.Create(1, "scripted");
        entity.AddComponent(new NativeScriptComponent { ScriptTypeName = "Test" });
        context.Register(entity);
        var scriptEngine = Substitute.For<IScriptEngine>();

        NativeScriptIteration.ProcessEvent(context, store, new KeyPressedEvent(KeyCodes.A, isRepeat: false));

        scriptEngine.DidNotReceive().CreateScriptInstance(Arg.Any<string>());
        store.TryGet(entity.Id, out _).ShouldBeFalse();
    }

    private sealed class TrackingScript : ScriptableEntity
    {
        public bool DestroyCalled { get; private set; }

        public TrackingScript() : base(new ComponentAccessor(), Substitute.For<IAudio>(), Substitute.For<IAudioPlayback>(), Substitute.For<IPhysicsQueries>(), NullEntityHierarchy.Instance) { }

        public override void OnDestroy() => DestroyCalled = true;
    }
}
