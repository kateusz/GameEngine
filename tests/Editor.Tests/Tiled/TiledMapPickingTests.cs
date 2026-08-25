using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.Tiled;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Rendering;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Editor.Tests.Tiled;

public class TiledMapPickingTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    [Fact]
    public void Resolve_PrefersChildUnderCursor_OverTilemap()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        scene.UpdateWorldTransforms();
        var child = scene.GetChildren(map).Single();

        var hit = TiledMapPicking.Resolve(scene, map, new Vector2(0.5f, 1.5f));
        hit.ShouldBe(child);
    }

    [Fact]
    public void Resolve_PicksNearestChild_WhenCursorMissesAabbs()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        scene.UpdateWorldTransforms();
        var child = scene.GetChildren(map).Single();

        TiledMapPicking.Resolve(scene, map, new Vector2(1.5f, 0.5f)).ShouldBe(child);
    }

    [Fact]
    public void Resolve_KeepsTilemap_WhenNoChildren()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        scene.DestroyEntity(scene.GetChildren(map).Single());
        scene.UpdateWorldTransforms();

        TiledMapPicking.Resolve(scene, map, new Vector2(0.5f, 0.5f)).ShouldBe(map);
    }

    private EngineScene CreateScene() =>
        new("test-scene", "test-scene", new Context(),
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);
}
