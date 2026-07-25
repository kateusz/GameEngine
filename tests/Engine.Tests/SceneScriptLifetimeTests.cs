using Audio;
using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using NSubstitute;
using SceneComponents;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests;

public class SceneScriptLifetimeTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    private EngineScene CreateScene(out ScriptRuntimeStore store, out Context context)
    {
        store = new ScriptRuntimeStore();
        context = new Context();
        return new EngineScene("test", "test", context,
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(), store, null!,
            NullCameraQueries.Instance);
    }

    [Fact]
    public void DestroyEntity_RemovesScriptFromStore_AndCallsOnDestroy()
    {
        using var scene = CreateScene(out var store, out _);
        var entity = scene.CreateEntity("scripted");
        entity.AddComponent(new NativeScriptComponent { ScriptTypeName = "Test" });
        var script = new TrackingScript();
        store.Set(entity.Id, script);

        scene.DestroyEntity(entity);

        store.TryGet(entity.Id, out _).ShouldBeFalse();
        script.DestroyCalled.ShouldBeTrue();
    }

    [Fact]
    public void DestroyEntity_IdReuse_DoesNotReturnStaleScript()
    {
        using var scene = CreateScene(out var store, out _);
        var entity = scene.CreateEntity("original");
        var staleScript = new TrackingScript();
        store.Set(entity.Id, staleScript);

        var entityId = entity.Id;
        scene.DestroyEntity(entity);

        var newEntity = Entity.Create(entityId, "reused-id");
        scene.AddEntity(newEntity);

        store.TryGet(entityId, out var found).ShouldBeFalse();
        found.ShouldNotBeSameAs(staleScript);
    }

    [Fact]
    public void ScriptUpdateSystem_OnShutdown_CallsOnDestroyAndClearsStore()
    {
        var scriptEngine = Substitute.For<IScriptEngine>();
        var context = new Context();
        var store = new ScriptRuntimeStore();
        var entity = Entity.Create(1, "scripted");
        entity.AddComponent(new NativeScriptComponent { ScriptTypeName = "Test" });
        context.Register(entity);
        var script = new TrackingScript();
        store.Set(entity.Id, script);
        var system = new ScriptUpdateSystem(context, scriptEngine, store);

        system.OnShutdown();

        script.DestroyCalled.ShouldBeTrue();
        store.TryGet(1, out _).ShouldBeFalse();
    }

    private sealed class TrackingScript : ScriptableEntity
    {
        public bool DestroyCalled { get; private set; }

        public TrackingScript() : base(new ComponentAccessor(), Substitute.For<IAudio>(), Substitute.For<IAudioPlayback>(), Substitute.For<IPhysicsQueries>(), NullEntityHierarchy.Instance) { }

        public override void OnDestroy() => DestroyCalled = true;
    }
}
