using System.Numerics;
using Engine.Project;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;
using ImGuiNET;
using SceneComponents.Camera;

namespace Benchmark;

public sealed class Benchmark3DRunner(SceneFactory sceneFactory, IGraphics2D graphics2D, IGraphics3D graphics3D,
    ITextureFactory textures, IModelFactory models, ISceneSerializer scenes) : IBenchmarkRunner
{
    const string PlantsScene = @"C:\Users\matku\Documents\game-engine-projects\3d\assets\scenes\rosliny.scene";
    private int _count = 1000;
    private float _duration = 5f;

    public string Title => "3D";
    public BenchmarkSettings Settings => new(_count, _duration);

    public IReadOnlyList<BenchmarkTestButton> Tests { get; } =
    [
        new("Cubes", "Cube count"),
        new("InstancedMesh", "Instanced mesh"),
        new("UniqueMeshes", "Unique meshes"),
        new("GlbModels", "GLB models")
    ];

    public void DrawControls()
    {
        var max = 20000;
        ImGui.DragInt("Object Count", ref _count, 100, 100, max);
        _count = System.Math.Clamp(_count, 100, max);
        ImGui.DragFloat("Test Duration (s)", ref _duration, 0.5f, 1f, 60f);
    }

    public bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error)
    {
        run = null;
        ScenePlan plan;
        try
        {
            plan = ScenePlans.Plan("3D", testId);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = $"Unknown test: {testId}";
            return false;
        }
        _duration = settings.DurationSeconds;
        if (testId == "GlbModels")
            return TryCreatePlants(plan, out run, out error);

        var count = testId == "UniqueMeshes"
            ? System.Math.Clamp(settings.Count, 100, BenchmarkScenes.UniqueMeshCap)
            : System.Math.Clamp(settings.Count, 100, 20000);
        _count = count;

        var scene = sceneFactory.Create("Benchmark", SceneDimension.ThreeD);
        BenchmarkScenes.AddAmbient(scene);

        if (testId == "Cubes")
        {
            BenchmarkScenes.AddDistrict(scene, count);
        }
        else if (testId == "InstancedMesh")
        {
            var path = PathBuilder.Resolve(BenchmarkScenes.TemplateAsset);
            if (models.Create(path, null) == null)
            {
                scene.Dispose();
                error = $"Mesh failed to load: {path}";
                return false;
            }

            BenchmarkScenes.AddDistrict(scene, count, path);
        }
        else if (testId == "UniqueMeshes")
        {
            byte[] bytes;
            try
            {
                var template = PathBuilder.Resolve(BenchmarkScenes.TemplateAsset);
                if (models.Create(template, null) == null)
                {
                    scene.Dispose();
                    error = $"Mesh failed to load: {template}";
                    return false;
                }

                bytes = File.ReadAllBytes(template + ".mesh");
            }
            catch (Exception ex)
            {
                scene.Dispose();
                error = ex.Message;
                return false;
            }

            if (!BenchmarkScenes.TryAddUniqueMeshes(scene, models, count, bytes, out error))
                return false;
        }
        else
        {
            scene.Dispose();
            error = $"Unknown test: {testId}";
            return false;
        }

        run = new PipelineBenchmarkRun(
            plan.ResultName, _duration, scene,
            plan.DirectionalShadows, plan.PointShadows,
            graphics2D, graphics3D, textures, models,
            clearModelCache: testId == "UniqueMeshes",
            ExpectedShadow.None);
        error = null;
        return true;
    }

    bool TryCreatePlants(ScenePlan plan, out BenchmarkRun? run, out string? error)
    {
        run = null;
        var scene = sceneFactory.Create("rosliny", SceneDimension.ThreeD);
        try
        {
            scenes.Deserialize(scene, PlantsScene);
        }
        catch (Exception ex)
        {
            scene.Dispose();
            error = ex.Message;
            return false;
        }

        scene.UpdateWorldTransforms();
        var pipeline = new PipelineBenchmarkRun(
            plan.ResultName, _duration, scene,
            plan.DirectionalShadows, plan.PointShadows,
            graphics2D, graphics3D, textures, models,
            clearModelCache: false,
            ExpectedShadow.None);
        if (TryPrimaryLook(scene, out var eye, out var focal))
            pipeline.Frame(eye, focal);
        run = pipeline;
        error = null;
        return true;
    }

    static bool TryPrimaryLook(IScene scene, out Vector3 eye, out Vector3 focal)
    {
        eye = default;
        focal = default;
        foreach (var entity in scene.Entities)
        {
            if (!entity.TryGetComponent<CameraComponent>(out var camera) || !camera.Primary)
                continue;
            if (!entity.TryGetComponent<SceneComponents.TransformComponent>(out var transform))
                continue;

            var world = transform.GetWorldTransform();
            eye = new Vector3(world.M41, world.M42, world.M43);
            var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, world));
            focal = eye + forward * 8f;
            return true;
        }

        return false;
    }
}
