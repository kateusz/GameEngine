using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Benchmark;

public class BenchmarkProjectSceneLoadTests
{
    static readonly string LightingShowcaseScene = RepoPath("Editor", "assets", "scenes", "lighting-showcase.scene");

    [Fact]
    public void Editor_lighting_showcase_deserializes_as_3d()
    {
        File.Exists(LightingShowcaseScene).ShouldBeTrue();

        using var scene = CreateEmptyScene();
        Serializer().Deserialize(scene, LightingShowcaseScene);

        scene.Dimension.ShouldBe(SceneDimension.ThreeD);
        scene.Entities.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void BenchmarkProjectAccess_finds_editor_root_from_scene_path()
    {
        global::Benchmark.BenchmarkProjectAccess.TryGuessProjectRoot(LightingShowcaseScene, out var root).ShouldBeTrue();
        Directory.Exists(Path.Combine(root, "assets")).ShouldBeTrue();
        Path.GetFullPath(root).ShouldEndWith($"{Path.DirectorySeparatorChar}Editor");
    }

    private static string RepoPath(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine([dir.FullName, ..parts]);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found");
    }

    private static SceneSerializer Serializer() =>
        new(new ComponentSerializerRegistry(), new SerializerOptions());

    private static EngineScene CreateEmptyScene() =>
        new("t", new Context(),
            new SystemManager(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            null!,
            NullCameraQueries.Instance);
}
