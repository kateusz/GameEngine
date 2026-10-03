using System.Numerics;
using Benchmark;
using ECS;
using Engine.Project;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Scene;
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
    public void Plan_matches_lighting_and_shadow_table()
    {
        ScenePlans.Plan("3D", "Cubes").ShouldBe(new ScenePlan("3D_Cubes", false, false, false, 0, false));
        ScenePlans.Plan("3D", "InstancedMesh").ShouldBe(new ScenePlan("3D_InstancedMesh", false, false, false, 0, false));
        ScenePlans.Plan("3D", "UniqueMeshes").ShouldBe(new ScenePlan("3D_UniqueMeshes", false, false, false, 0, false));
        ScenePlans.Plan("3D", "GlbModels").ShouldBe(new ScenePlan("3D_GlbModels", true, true, false, 0, false));
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
