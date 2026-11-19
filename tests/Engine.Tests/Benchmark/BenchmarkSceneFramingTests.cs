using System.Numerics;
using Benchmark;
using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Benchmark;

public class BenchmarkSceneFramingTests
{
    [Theory]
    [InlineData(0f, 4f)]
    [InlineData(1f, 4f)]
    [InlineData(8f, 15.2f)]
    [InlineData(1000f, 64f)]
    public void Framing_distance_clamps_to_near_plane_and_old_pullback(float radius, float expected) =>
        BenchmarkCamera.FramingDistance(radius).ShouldBe(expected, tolerance: 0.01f);

    [Fact]
    public void Small_scene_frames_closer_than_the_old_64_unit_pullback()
    {
        using var scene = SceneWithPlantAt(new Vector3(0f, 0f, 5f));

        Benchmark3DRunner.TryFrameScene(scene, out var eye, out var focal).ShouldBeTrue();

        Vector3.Distance(focal, new Vector3(0f, 0f, 5f)).ShouldBeLessThan(0.01f);
        Vector3.Distance(eye, focal).ShouldBe(BenchmarkCamera.FramingDistance(0f), tolerance: 0.01f);
    }

    [Fact]
    public void Scene_without_models_is_not_framed()
    {
        using var scene = SceneWithPlantAt(null);

        Benchmark3DRunner.TryFrameScene(scene, out _, out _).ShouldBeFalse();
    }

    static EngineScene SceneWithPlantAt(Vector3? plantPosition)
    {
        var scene = new EngineScene("t", new Context(), new SystemManager(),
            new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(), null!, NullCameraQueries.Instance);

        var camera = scene.CreateEntity("Camera");
        camera.AddComponent(new TransformComponent { Translation = Vector3.Zero });
        camera.AddComponent(new CameraComponent { Primary = true });

        if (plantPosition is { } position)
        {
            var plant = scene.CreateEntity("plant");
            plant.AddComponent(new TransformComponent { Translation = position });
            plant.AddComponent(new ModelRendererComponent { ModelPath = "/rosliny.glb" });
        }

        scene.UpdateWorldTransforms();
        return scene;
    }
}
