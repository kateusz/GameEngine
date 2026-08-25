using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.Tiled;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Editor.Tests.Tiled;

public class TiledMapApplierTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    [Fact]
    public void CreateMap_RectangleGetsBoxCollider()
    {
        using var scene = CreateScene();
        var data = TiledTestMaps.ParseRect();
        var map = TiledMapApplier.CreateMap(scene, data, "maps/a.tmj", "a");

        var child = scene.GetChildren(map).Single();
        child.TryGetComponent<BoxCollider2DComponent>(out var box).ShouldBeTrue();
        box!.Size.ShouldBe(new Vector2(0.5f, 0.5f));
        box.IsTrigger.ShouldBeTrue();
        child.GetComponent<TiledObjectComponent>().TiledId.ShouldBe(1);
    }

    [Fact]
    public void Reimport_KeepsExtraComponent_UpdatesPose()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        var child = scene.GetChildren(map).Single();
        child.AddComponent(new NativeScriptComponent { ScriptTypeName = "Games.Demo.Player" });

        var moved = TiledTestMaps.ParseRect(x: 16, y: 0);
        TiledMapApplier.Reimport(scene, map, moved, "maps/a.tmj");

        var after = scene.GetChildren(map).Single();
        after.GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("Games.Demo.Player");
        after.GetComponent<TransformComponent>().Translation.X.ShouldBe(1.5f, 0.0001f);
    }

    [Fact]
    public void Reimport_RemovesMissingIds_AddsNew_LeavesUnmarked()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        var helper = scene.CreateEntity("helper");
        helper.AddComponent(new TransformComponent());
        scene.SetParent(helper, map);

        TiledMapApplier.Reimport(scene, map, TiledTestMaps.ParseRect(id: 9, name: "spawn"), "maps/a.tmj");

        var children = scene.GetChildren(map);
        children.Count.ShouldBe(2);
        children.ShouldContain(c => c.Name == "helper");
        children.Select(c => c.TryGetComponent<TiledObjectComponent>(out var m) ? m.TiledId : -1)
            .ShouldContain(9);
        children.Select(c => c.TryGetComponent<TiledObjectComponent>(out var m) ? m.TiledId : -1)
            .ShouldNotContain(1);
    }

    [Fact]
    public void Reimport_RemovesStaleShapeComponents()
    {
        using var scene = CreateScene();
        var map = TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
        var child = scene.GetChildren(map).Single();
        child.HasComponent<BoxCollider2DComponent>().ShouldBeTrue();
        child.HasComponent<RigidBody2DComponent>().ShouldBeTrue();

        var pointOnly = TiledTestMaps.ParseMapJson("""
            {
              "orientation":"orthogonal","infinite":false,
              "width":2,"height":2,"tilewidth":16,"tileheight":16,
              "tilesets":[{"firstgid":1,"source":"tiles.tsj"}],
              "layers":[
                {"type":"tilelayer","name":"ground","width":2,"height":2,"data":[0,0,0,0]},
                {"type":"objectgroup","name":"obj","objects":[{"id":1,"name":"wall","x":0,"y":0}]}
              ]
            }
            """).Result!;

        TiledMapApplier.Reimport(scene, map, pointOnly, "maps/a.tmj");

        child = scene.GetChildren(map).Single();
        child.HasComponent<BoxCollider2DComponent>().ShouldBeFalse();
        child.HasComponent<RigidBody2DComponent>().ShouldBeFalse();
        child.HasComponent<SubTextureRendererComponent>().ShouldBeFalse();
    }

    [Fact]
    public void SceneRoundTrip_PreservesLayersAndMarker()
    {
        var registry = new ComponentSerializerRegistry();
        var serializer = new SceneSerializer(registry, new SerializerOptions());
        var path = Path.Combine(Path.GetTempPath(), $"tm-{Guid.NewGuid():N}.scene");
        try
        {
            using (var scene = CreateScene())
            {
                TiledMapApplier.CreateMap(scene, TiledTestMaps.ParseRect(), "maps/a.tmj", "a");
                serializer.Serialize(scene, path);
            }

            using var loaded = CreateScene();
            serializer.Deserialize(loaded, path);
            var map = loaded.Entities.Single(e => e.HasComponent<TileMapComponent>());
            var tilemap = map.GetComponent<TileMapComponent>();
            tilemap.SourceMapPath.ShouldBe("maps/a.tmj");
            tilemap.Layers[0].Tiles.ShouldContain(-1);
            var marker = loaded.GetChildren(map).Single().GetComponent<TiledObjectComponent>();
            marker.TiledId.ShouldBe(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private EngineScene CreateScene() =>
        new("test-scene", "test-scene", new Context(),
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);
}
