using Audio;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
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
    private readonly IGraphics2D _mockGraphics2D = Substitute.For<IGraphics2D>();
    private readonly IGraphics3D _mockGraphics3D = Substitute.For<IGraphics3D>();
    private readonly ITextureFactory _mockTextureFactory = Substitute.For<ITextureFactory>();
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    private EngineScene CreateScene(out ScriptRuntimeStore store)
    {
        store = new ScriptRuntimeStore();
        return new EngineScene("test", "test", _mockGraphics2D, _mockGraphics3D, _mockTextureFactory, new Context(),
            new Core.DebugSettings(), _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(), store);
    }

    [Fact]
    public void DestroyEntity_RemovesScriptFromStore_AndCallsOnDestroy()
    {
        using var scene = CreateScene(out var store);
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
        using var scene = CreateScene(out var store);
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
    public void ScriptUpdateSystem_OnShutdown_CallsOnRuntimeStopAndClearsStore()
    {
        var scriptEngine = Substitute.For<IScriptEngine>();
        var store = new ScriptRuntimeStore();
        store.Set(1, new TrackingScript());
        var system = new ScriptUpdateSystem(scriptEngine, store);

        system.OnShutdown();

        scriptEngine.Received(1).OnRuntimeStop(store);
        store.TryGet(1, out _).ShouldBeFalse();
    }

    private sealed class TrackingScript : ScriptableEntity
    {
        public bool DestroyCalled { get; private set; }

        public TrackingScript() : base(new ComponentAccessor(), Substitute.For<IAudio>(), Substitute.For<IAudioPlayback>()) { }

        public override void OnDestroy() => DestroyCalled = true;
    }
}
