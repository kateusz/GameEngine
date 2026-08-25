using ECS;
using ECS.Systems;
using Editor.Features.History.Commands;
using Editor.Features.Tiled;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Editor.Tests.Tiled;

public class ReimportTiledMapCommandTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    [Fact]
    public void Execute_KeepsScript_FailedParseDoesNotRun()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(trigger: false), "maps/a.tmj", "a");
        var child = scene.GetChildren(map).Single();
        child.AddComponent(new NativeScriptComponent { ScriptTypeName = "KeepMe" });

        var cmd = new ReimportTiledMapCommand(scene, map, TiledTestMaps.ParseRect(trigger: false), "maps/a.tmj");
        cmd.Execute().ShouldBeTrue();
        scene.GetChildren(map).Single().GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("KeepMe");
    }

    [Fact]
    public void Undo_RestoresRemovedChild()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(trigger: false), "maps/a.tmj", "a");
        var cmd = new ReimportTiledMapCommand(scene, map, TiledTestMaps.ParseRect(id: 2, name: "other", trigger: false), "maps/a.tmj");
        cmd.Execute();
        scene.GetChildren(map).Single().GetComponent<TiledObjectComponent>().TiledId.ShouldBe(2);

        cmd.Undo();
        scene.GetChildren(map).Single().GetComponent<TiledObjectComponent>().TiledId.ShouldBe(1);
    }

    private EngineScene CreateScene() =>
        new("test-scene", "test-scene", new Context(),
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);
}
