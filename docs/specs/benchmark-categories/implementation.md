# Benchmark Categories — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The 2D test bodies move as they are. Do not re-implement them.

## 1. View flag

`SceneView` gains the switch at the end, so every current call site keeps its meaning.

```csharp
public readonly record struct SceneView(
    Matrix4x4 ViewProjection,
    Vector3 ViewPosition = default,
    bool PointShadows = true,
    float DirectionalShadowCasterMaxDistance = LightingMath.ShadowDistance,
    bool DirectionalShadows = true);
```

In `SceneRenderPipeline.Render3D`, the depth pass already starts by turning the shadow off. Keep that. Gate the fit:

```csharp
graphics3D.SetDirectionalShadow(Matrix4x4.Identity, false);
if (view.DirectionalShadows &&
    lightColor != Vector3.Zero &&
    LightingMath.TryFitDirectionalShadow(view.ViewProjection, lightDirection, out var lightViewProjection))
{
    perf.DirectionalShadow = true;
    graphics3D.BeginShadowPass(lightViewProjection);
    perf.DirectionalShadowPass = DrawOpaque3D(
        context, graphics3D, textureFactory, modelFactory, lightViewProjection,
        shadowCasterMaxSq, shadowCasterView);
    graphics3D.EndShadowPass();
    graphics3D.SetDirectionalShadow(lightViewProjection, true);
}
```

The warning branch stays, and it also requires `view.DirectionalShadows`. A lighting run must not log a fit failure it asked to skip.

## 2. Pipeline test

Add this beside `RenderScene_DirectionalLight_DrawsCubeInBothPasses`. The default-on case is that existing test. Do not change it.

```csharp
[Fact]
public void RenderScene_DirectionalLight_DirectionalShadowsDisabled_SkipsShadowPass()
{
    var context = new Context();
    var sun = new Entity(1, "sun");
    sun.AddComponent(new DirectionalLightComponent
    {
        Direction = new Vector3(0.3f, -1f, 0.2f),
        Color = Vector4.One,
        Intensity = 1f
    });
    context.Register(sun);

    var graphics = new RecordingGraphics3D();
    SceneRenderPipeline.RenderScene(
        context,
        Substitute.For<IGraphics2D>(),
        graphics,
        Substitute.For<ITextureFactory>(),
        Substitute.For<IModelFactory>(),
        new SceneView(ViewProjection(new Vector3(0f, 12f, 18f)), DirectionalShadows: false));

    graphics.ShadowPasses.ShouldBeEmpty();
    graphics.BeginScenes.ShouldBe(1);
    graphics.Shadows.ShouldBe([(Matrix4x4.Identity, false)]);
}
```

## 3. Run object

The layer owns ImGui, the clock, and the result list. A run is the scene. `TryCreate` is what the tests call, so the shadow flags can be asserted without a window.

```csharp
namespace Benchmark;

public abstract class BenchmarkRun : IDisposable
{
    public abstract string ResultName { get; }
    public abstract void Tick(TimeSpan delta);
    public abstract void Contribute(BenchmarkResult result);
    public abstract void Dispose();
}

public readonly record struct BenchmarkSettings(int Count, float DurationSeconds);

public interface IBenchmarkRunner
{
    string Title { get; }
    bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error);
    IReadOnlyList<string> TestIds { get; }
}
```

`BenchmarkLayer` holds `IBenchmarkRunner? _category` and `BenchmarkRun? _run`. Opening screen: `_category` is null. Back sets both to null after disposing `_run` if it exists. Dispose is idempotent. A second Back must not dispose twice. Do that with a disposed flag inside each run, or by nulling `_run` immediately after the first dispose.

Start:

```csharp
if (!_category!.TryCreate(testId, Settings(), out var run, out var error))
{
    _status = error;
    return;
}

_status = null;
_run = run;
ResetSamples();
```

Each measured frame:

```csharp
_run.Tick(timeSpan);
RecordFrameTime((float)_frameTimer.Elapsed.TotalMilliseconds);
```

Stop and natural end call `Finish(commit: true)`. Back calls `Finish(commit: false)`.

```csharp
private void Finish(bool commit)
{
    if (_run == null)
        return;

    if (commit)
        Commit(_run);

    _run.Dispose();
    _run = null;
}
```

`Commit` is today's `FinalizeBenchmark`, with the result name taken from `_run.ResultName` and `_run.Contribute(result)` called before the result is appended. If the sample list is empty, commit stores nothing. That is the same guard the layer has today.

2D result names stay whatever `GetBenchmarkResultName` returns today. Move that method onto the 2D run.

## 4. 2D runner

Move the existing setup, update, and direct-draw methods onto `Benchmark2DRunner` without changing their numbers or their draw path. `TryCreate` switches on the five existing test ids. The physics run is the only one that calls `OnRuntimeStart`, and its `Dispose` is the only one that calls `OnRuntimeStop`.

`Contribute` keeps today's `Graphics2DStats` aggregation. Do not point the 2D tests at `SceneRenderPipeline`.

## 5. Shared 3D frame

```csharp
public sealed class PipelineBenchmarkRun : BenchmarkRun
{
    private readonly IScene _scene;
    private readonly SceneView _view;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ITextureFactory _textures;
    private readonly IModelFactory _models;
    private readonly bool _clearModelCache;
    private bool _disposed;
    private Statistics _last = new();

    public override string ResultName { get; }

    public override void Tick(TimeSpan delta)
    {
        _graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1f));
        _graphics2D.Clear();
        _graphics3D.ResetStats();
        SceneRenderPipeline.RenderScene(
            _scene.Context, _graphics2D, _graphics3D, _textures, _models, _view);
        _last = _graphics3D.GetStats();
    }

    public override void Contribute(BenchmarkResult result)
    {
        result.CustomMetrics["Color draw calls"] = _last.ColorDrawCalls.ToString();
        result.CustomMetrics["Instanced draws"] = _last.InstancedDraws.ToString();
        result.CustomMetrics["Instances"] = _last.Instances.ToString();
        result.CustomMetrics["Point lights"] = _last.PointLights.ToString();
        result.CustomMetrics["Point shadow lights"] = _last.PointShadowLights.ToString();
        result.CustomMetrics["Directional shadow"] = _last.DirectionalShadow ? "on" : "off";
    }

    public override void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _scene.Dispose();
        if (_clearModelCache)
            _models.Clear();
    }
}
```

The view is built once when the run is created. Aspect is `DisplayConfig.DefaultAspectRatio` (1280/720). A resize during the run does not rebuild it.

```csharp
private static SceneView MakeView(bool directionalShadows, bool pointShadows)
{
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(
        MathF.PI / 3f, DisplayConfig.DefaultAspectRatio, 0.1f, 100f);
    var eye = new Vector3(0f, 12f, 18f);
    var view = Matrix4x4.CreateLookAt(eye, Vector3.Zero, Vector3.UnitY);
    return new SceneView(view * projection, eye, pointShadows, DirectionalShadows: directionalShadows);
}
```

## 6. Cubes, copies, and identities

Empty `ModelPath` is a unit cube. The template is one glTF next to the benchmark, read once into a `byte[]`.

```csharp
private static void AddCube(IScene scene, Vector3 position)
{
    var entity = scene.CreateEntity("cube");
    var transform = entity.AddComponent<TransformComponent>();
    transform.Translation = position;
    entity.AddComponent<ModelRendererComponent>();
}

private static bool TryAddMesh(IScene scene, IModelFactory models, Vector3 position,
    string path, byte[] bytes, out string? error)
{
    error = null;
    if (models.Create(path, bytes) == null)
    {
        error = $"Mesh failed to load: {path}";
        return false;
    }

    var entity = scene.CreateEntity("mesh");
    var transform = entity.AddComponent<TransformComponent>();
    transform.Translation = position;
    entity.AddComponent(new ModelRendererComponent { ModelPath = path });
    return true;
}
```

Instancing uses one path for every entity, the template's real path, and passes null bytes so the factory reads the file. Unique meshes use `benchmark-unique-{index}.gltf` and the same byte array. On the first null, dispose the scene, call `models.Clear()`, and return the error. The unique run sets `_clearModelCache` so a successful run also clears on dispose. The other runs leave the cache.

Object positions for the 3D count slider: a square grid, spacing 1.5, centered, y = 0. The slider is 100 to 20000, default 1000, except unique meshes, which stop at 500. Selecting unique meshes clamps a count above 500 down to 500.

Ambient only, on every 3D run:

```csharp
var ambient = scene.CreateEntity("ambient");
ambient.AddComponent(new AmbientLightComponent { Color = Vector4.One, Strength = 0.1f });
```

No directional entity. No point lights. Both view flags false.

## 7. Lighting and shadow grid

256 cubes, 16 by 16, spacing 1.5, centered. Then one of the six light setups.

```csharp
private const int GridCount = 256;
private const int GridColumns = 16;
private const float GridSpacing = 1.5f;

private static void AddSun(IScene scene)
{
    var sun = scene.CreateEntity("sun");
    sun.AddComponent(new DirectionalLightComponent
    {
        Direction = new Vector3(0.3f, -1f, 0.2f),
        Color = Vector4.One,
        Intensity = 1f
    });
}

private static void AddLamps(IScene scene, int count, bool castsShadow)
{
    for (var i = 0; i < count; i++)
    {
        var column = i % 4;
        var row = i / 4;
        var lamp = scene.CreateEntity($"lamp_{i}");
        var transform = lamp.AddComponent<TransformComponent>();
        transform.Translation = new Vector3((column - 1.5f) * 6f, 4f, (row - 0.5f) * 6f);
        lamp.AddComponent(new PointLightComponent
        {
            Color = Vector4.One,
            Intensity = 1f,
            Range = 25f,
            CastsShadow = castsShadow
        });
    }
}
```

| testId | ResultName | Sun | Lamps | DirectionalShadows | PointShadows | CastsShadow |
|--------|------------|-----|-------|--------------------|--------------|-------------|
| sun | Lighting_Sun | yes | 0 | false | false | — |
| point1 | Lighting_Point1 | no | 1 | false | false | false |
| point8 | Lighting_Point8 | no | 8 | false | false | false |
| directional | Shadows_Directional | yes | 0 | true | false | — |
| point1 | Shadows_Point1 | no | 1 | false | true | true |
| point8 | Shadows_Point8 | no | 8 | false | true | true |

After `Contribute`, if the test is `Shadows_Directional` and `_last.DirectionalShadow` is false, set `Shadow pass` to `skipped`. If the test is a shadow point test and `_last.PointShadowLights` is 0, set the same metric.

## 8. Runner tests

One test class in the benchmark test project, or in `Engine.Tests` if the plan builder is a plain function that does not need ImGui. Prefer a function that returns the flags and the light counts without creating GPU resources, and let `TryCreate` call it before it touches the factory.

```csharp
public readonly record struct ScenePlan(
    string ResultName,
    bool DirectionalShadows,
    bool PointShadows,
    bool Sun,
    int PointLights,
    bool CastsShadow);

public static ScenePlan Plan(string category, string testId) => (category, testId) switch
{
    ("3D", "Cubes") => new ScenePlan("3D_Cubes", false, false, false, 0, false),
    ("3D", "InstancedMesh") => new ScenePlan("3D_InstancedMesh", false, false, false, 0, false),
    ("3D", "UniqueMeshes") => new ScenePlan("3D_UniqueMeshes", false, false, false, 0, false),
    ("Lighting", "sun") => new ScenePlan("Lighting_Sun", false, false, true, 0, false),
    ("Lighting", "point1") => new ScenePlan("Lighting_Point1", false, false, false, 1, false),
    ("Lighting", "point8") => new ScenePlan("Lighting_Point8", false, false, false, 8, false),
    ("Shadows", "directional") => new ScenePlan("Shadows_Directional", true, false, true, 0, false),
    ("Shadows", "point1") => new ScenePlan("Shadows_Point1", false, true, false, 1, true),
    ("Shadows", "point8") => new ScenePlan("Shadows_Point8", false, true, false, 8, true),
    _ => throw new ArgumentOutOfRangeException(nameof(testId))
};
```

Assert the six rows and that `Plan("Lighting", "point8").ResultName` differs from `Plan("Shadows", "point8").ResultName`.

The clock rule is a separate pure check: given a list of results, `Finish(commit: false)` leaves the count unchanged and `Finish(commit: true)` with a non-empty sample list adds one. Drive it with a fake run whose `Dispose` counts calls, and call finish twice. The second call disposes nothing.

Unique-mesh cleanup: a fake factory whose second `Create` returns null. After `TryCreate`, the scene is disposed and `Clear` has been called once.
