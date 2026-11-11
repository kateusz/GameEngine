using System.Numerics;
using Benchmark;
using ECS;
using ECS.Systems;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Tests.Scene;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Benchmark;

[Collection("PathBuilder")]
public class BenchmarkCategoryTests : IDisposable
{
    public BenchmarkCategoryTests()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(Path.GetTempPath());
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose() => PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());

    [Fact]
    public void Camera_left_orbits_middle_pans_scroll_zooms()
    {
        var camera = new BenchmarkCamera();
        camera.Eye.Y.ShouldBe(12f, 0.05f);
        camera.Eye.Z.ShouldBe(18f, 0.05f);

        camera.Button(0, true);
        camera.Move(Vector2.Zero);
        camera.Move(new Vector2(120f, 0f));
        System.Math.Abs(camera.Eye.X).ShouldBeGreaterThan(0.5f);
        camera.Focal.ShouldBe(Vector3.Zero);
        camera.Button(0, false);

        camera.Button(2, true);
        camera.Move(new Vector2(120f, 0f));
        camera.Move(new Vector2(240f, 0f));
        camera.Focal.ShouldNotBe(Vector3.Zero);
        camera.Button(2, false);

        var distance = camera.Distance;
        camera.Zoom(1f);
        camera.Distance.ShouldBeLessThan(distance);
    }

    [Fact]
    public void District_lots_vary_shape_and_material()
    {
        var lots = BenchmarkScenes.Lots(48).ToArray();
        lots.Length.ShouldBe(48);
        lots.Select(lot => lot.Scale.Y).Distinct().Count().ShouldBeGreaterThan(2);
        lots.Any(lot => lot.Metallic > 0.5f).ShouldBeTrue();
        lots.Any(lot => lot.Metallic == 0f).ShouldBeTrue();
        lots.Select(lot => lot.Color).Distinct().Count().ShouldBeGreaterThan(2);
        lots.Any(lot => lot.Position.Y > 1f).ShouldBeTrue();
    }

    [Fact]
    public void Guess_project_root_from_scene_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "bench-proj-" + Guid.NewGuid());
        var scenes = Path.Combine(root, "assets", "scenes");
        Directory.CreateDirectory(scenes);
        var scene = Path.Combine(scenes, "level.scene");
        File.WriteAllText(scene, "{}");

        try
        {
            BenchmarkProjectAccess.TryGuessProjectRoot(scene, out var found).ShouldBeTrue();
            found.ShouldBe(root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Plan_matches_lighting_and_shadow_table()
    {
        ScenePlans.Plan("3D", "Cubes").ShouldBe(new ScenePlan("3D_Cubes", false, false, false, 0, false));
        ScenePlans.Plan("3D", "InstancedMesh").ShouldBe(new ScenePlan("3D_InstancedMesh", false, false, false, 0, false));
        ScenePlans.Plan("3D", "UniqueMeshes").ShouldBe(new ScenePlan("3D_UniqueMeshes", false, false, false, 0, false));
        ScenePlans.Plan("3D", "GlbModels").ShouldBe(new ScenePlan("3D_GlbModels", true, false, false, 0, false));
        ScenePlans.Plan("Lighting", "sun").ShouldBe(new ScenePlan("Lighting_Sun", false, false, true, 0, false));
        ScenePlans.Plan("Lighting", "point1").ShouldBe(new ScenePlan("Lighting_Point1", false, false, false, 1, false));
        ScenePlans.Plan("Lighting", "point8").ShouldBe(new ScenePlan("Lighting_Point8", false, false, false, 8, false));
        ScenePlans.Plan("Shadows", "directional").ShouldBe(new ScenePlan("Shadows_Directional", true, false, true, 0, false));
        ScenePlans.Plan("Shadows", "point1").ShouldBe(new ScenePlan("Shadows_Point1", false, true, false, 1, true));
        ScenePlans.Plan("Shadows", "point8").ShouldBe(new ScenePlan("Shadows_Point8", false, true, false, 8, true));
        ScenePlans.Plan("Lighting", "point8").ResultName.ShouldNotBe(ScenePlans.Plan("Shadows", "point8").ResultName);
    }

    [Fact]
    public void Finish_commit_appends_one_result_and_second_call_does_not_dispose()
    {
        var active = new ActiveRun();
        var run = new FakeRun();
        active.Adopt(run);
        var results = new List<BenchmarkResult>();

        active.Finish(true, 2, finished =>
        {
            var result = new BenchmarkResult { TestName = finished.ResultName };
            finished.Contribute(result);
            results.Add(result);
        }).ShouldBeTrue();

        results.Count.ShouldBe(1);
        results[0].TestName.ShouldBe("fake");
        results[0].CustomMetrics["ok"].ShouldBe("1");

        active.Finish(true, 2, _ => results.Add(new BenchmarkResult())).ShouldBeFalse();
        results.Count.ShouldBe(1);
        run.Disposes.ShouldBe(1);
    }

    [Fact]
    public void Finish_abort_stores_nothing()
    {
        var active = new ActiveRun();
        var run = new FakeRun();
        active.Adopt(run);
        var results = new List<BenchmarkResult>();

        active.Finish(false, 3, finished => results.Add(new BenchmarkResult { TestName = finished.ResultName }))
            .ShouldBeFalse();

        results.ShouldBeEmpty();
        run.Disposes.ShouldBe(1);
        active.Finish(false, 3, _ => results.Add(new BenchmarkResult())).ShouldBeFalse();
        run.Disposes.ShouldBe(1);
    }

    [Fact]
    public void Unique_mesh_failure_disposes_scene_and_clears_cache()
    {
        var scene = Substitute.For<IScene>();
        scene.CreateEntity(Arg.Any<string>()).Returns(_ => new Entity(1, "mesh"));
        var models = Substitute.For<IModelFactory>();
        var loaded = new Model("ok", Array.Empty<Mesh>());
        models.Create(Arg.Any<string>(), Arg.Any<byte[]?>()).Returns(loaded, (Model?)null);

        var created = BenchmarkScenes.TryAddUniqueMeshes(scene, models, 2, [1], out var error);

        created.ShouldBeFalse();
        error.ShouldNotBeNull();
        scene.Received(1).Dispose();
        models.Received(1).Clear();
    }

    [Fact]
    public void Editor_display_tonemaps_when_the_hdr_target_exists()
    {
        EditorScenePresenter.Pixels(1280f, 2f).ShouldBe(2560u);
        EditorScenePresenter.Pixels(0f, 2f).ShouldBe(0u);

        var draws = 0;
        var tonemaps = 0;
        EditorScenePresenter.Show(1280, 720, tonemapReady: true, () => draws++, (w, h, draw) =>
        {
            w.ShouldBe(1280u);
            h.ShouldBe(720u);
            draw();
            tonemaps++;
            return true;
        });
        draws.ShouldBe(1);
        tonemaps.ShouldBe(1);

        draws = 0;
        EditorScenePresenter.Show(1280, 720, tonemapReady: true, () => draws++, (_, _, _) => false);
        draws.ShouldBe(1);

        draws = 0;
        var hdr = 0;
        EditorScenePresenter.Show(0, 720, tonemapReady: true, () => draws++, (_, _, _) =>
        {
            hdr++;
            return true;
        });
        draws.ShouldBe(1);
        hdr.ShouldBe(0);
    }

    [Fact]
    public void Project_scene_draws_inside_presenter_with_editor_viewport_shadows()
    {
        var scene = new Engine.Scene.Scene("t", new Context(), new SystemManager(),
            new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(), null!, NullCameraQueries.Instance);
        var inside = false;
        var graphics2D = new ClearProbe(() => inside);
        var graphics3D = new MeshDrawRecordingGraphics3D();
        using var run = new PipelineBenchmarkRun(
            "3D_GlbModels", 0f, scene, true, false,
            graphics2D, graphics3D,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(),
            false, ExpectedShadow.None,
            display: new ScopePresenter(
                () => inside = true,
                () => inside = false),
            matchEditorLight: true);

        run.Tick(TimeSpan.FromMilliseconds(16));

        graphics2D.ClearedInsidePresenter.ShouldBeTrue();
        graphics3D.ShadowQualitySets.ShouldBe(1);
        graphics3D.ShadowQuality.ShouldBe(ShadowQuality.For(ShadowPreset.Low));
        graphics3D.LastView.ShouldNotBeNull();
        graphics3D.LastView!.Value.DirectionalShadows.ShouldBeTrue();
        graphics3D.LastView.Value.PointShadows.ShouldBeFalse();
    }

    private sealed class ScopePresenter(Action enter, Action leave) : IScenePresenter
    {
        public void Present(Action drawScene)
        {
            enter();
            drawScene();
            leave();
        }
    }

    private sealed class ClearProbe(Func<bool> inside) : IGraphics2D
    {
        public bool ClearedInsidePresenter { get; private set; }
        public void SetClearColor(Vector4 color) => ClearedInsidePresenter = inside();
        public void Clear() { }
        public void Init() { }
        public void Dispose() { }
        public void BeginScene(in SceneView view) { }
        public void EndScene() { }
        public void DrawQuad(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawQuad(Vector3 position, Vector2 size, Vector4 color) { }
        public void DrawQuad(Vector3 position, Vector2 size, float rotation, SubTexture2D subTexture) { }
        public void DrawQuad(Vector2 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f, Vector4? tintColor = null) { }
        public void DrawQuad(Vector3 position, Vector2 size, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null) { }
        public void DrawQuad(Vector3 position, Vector2 size, float rotation, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null) { }
        public void DrawQuad(Matrix4x4 transform, Vector4 color, int entityId = -1) { }
        public void DrawQuad(Matrix4x4 transform, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null, int entityId = -1) { }
        public void DrawLine(Vector3 p0, Vector3 p1, Vector4 color, int entityId) { }
        public void DrawRect(Vector3 position, Vector2 size, Vector4 color, int entityId) { }
        public void DrawRect(Matrix4x4 transform, Vector4 color, int entityId) { }
        public void ResetStats() { }
        public Graphics2DStats GetStats() => new();
    }

    private sealed class FakeRun : BenchmarkRun
    {
        public int Disposes { get; private set; }
        public override string ResultName => "fake";
        public override float DurationSeconds => 1f;
        public override void Tick(TimeSpan delta) { }
        public override void Contribute(BenchmarkResult result) => result.CustomMetrics["ok"] = "1";
        public override void Dispose() => Disposes++;
    }
}
