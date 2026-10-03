using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Benchmark;

public sealed class BenchmarkGridRunner(
    string category,
    SceneFactory sceneFactory,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textures,
    IModelFactory models) : IBenchmarkRunner
{
    private float _duration = 5f;

    private static readonly BenchmarkTestButton[] LightingTests =
    [
        new("sun", "Sun"),
        new("point1", "One point light"),
        new("point8", "Eight point lights")
    ];

    private static readonly BenchmarkTestButton[] ShadowTests =
    [
        new("directional", "Directional casters"),
        new("point1", "One point light shadow"),
        new("point8", "Eight point light shadows")
    ];

    public string Title => category;
    public BenchmarkSettings Settings => new(BenchmarkScenes.DistrictObjects, _duration);
    public IReadOnlyList<BenchmarkTestButton> Tests => category == "Lighting" ? LightingTests : ShadowTests;

    public void DrawControls()
    {
        ImGui.DragFloat("Test Duration (s)", ref _duration, 0.5f, 1f, 60f);
    }

    public bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error)
    {
        run = null;
        ScenePlan plan;
        try
        {
            plan = ScenePlans.Plan(category, testId);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = $"Unknown test: {testId}";
            return false;
        }

        _duration = settings.DurationSeconds;
        var scene = sceneFactory.Create("Benchmark", SceneDimension.ThreeD);
        BenchmarkScenes.AddDistrict(scene, BenchmarkScenes.DistrictObjects);
        if (plan.Sun)
            BenchmarkScenes.AddSun(scene);
        if (plan.PointLights > 0)
            BenchmarkScenes.AddLamps(scene, plan.PointLights, plan.CastsShadow);

        var expected = plan.PointLights > 0 && plan.CastsShadow
            ? ExpectedShadow.Point
            : plan.Sun && plan.DirectionalShadows
                ? ExpectedShadow.Directional
                : ExpectedShadow.None;

        run = new PipelineBenchmarkRun(
            plan.ResultName, _duration, scene,
            plan.DirectionalShadows, plan.PointShadows,
            graphics2D, graphics3D, textures, models,
            clearModelCache: false,
            expected);
        error = null;
        return true;
    }
}
