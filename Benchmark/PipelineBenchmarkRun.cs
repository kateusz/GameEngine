using System.Numerics;
using Engine.Events.Input;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;

namespace Benchmark;

public enum ExpectedShadow
{
    None,
    Directional,
    Point
}

public sealed class PipelineBenchmarkRun : BenchmarkRun
{
    /// <summary>
    /// Shadow settings as the editor UI sees them, so a perf run measures the cost the user
    /// actually pays. Defaults mirror a fresh editor install.
    /// </summary>
    public sealed record ShadowSettings(
        int DirectionalResolution = 1024,
        int PointResolution = 512,
        float Distance = 50f,
        int PcfTaps = 1,
        bool PointPcf = false,
        int Cascades = 1)
    {
        public ShadowQuality ToQuality() =>
            new ShadowQuality(DirectionalResolution, PointResolution, Distance, PcfTaps, PointPcf, Cascades).Sanitized();

        /// <summary>
        /// Reads the editor's saved preferences so a benchmark matches this machine's viewport.
        /// Missing or corrupt file falls back to the fresh-install defaults.
        /// </summary>
        public static ShadowSettings FromEditorPreferences(string preferencesPath)
        {
            try
            {
                if (!File.Exists(preferencesPath))
                    return new ShadowSettings();

                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(preferencesPath));
                var root = doc.RootElement;
                return new ShadowSettings(
                    Read(root, "SunShadowResolution", 1024),
                    Read(root, "PointShadowResolution", 512),
                    Read(root, "ShadowDistance", 50f),
                    Read(root, "ShadowPcf", 1),
                    Read(root, "PointShadowPcf", false),
                    Read(root, "ShadowCascades", 1));
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
            {
                return new ShadowSettings();
            }
        }

        static int Read(System.Text.Json.JsonElement root, string name, int fallback) =>
            root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

        static float Read(System.Text.Json.JsonElement root, string name, float fallback) =>
            root.TryGetProperty(name, out var value) && value.TryGetSingle(out var parsed) ? parsed : fallback;

        static bool Read(System.Text.Json.JsonElement root, string name, bool fallback) =>
            root.TryGetProperty(name, out var value) && value.ValueKind is System.Text.Json.JsonValueKind.True
                or System.Text.Json.JsonValueKind.False
                ? value.GetBoolean()
                : fallback;
    }

    private readonly SceneRenderPipeline _pipeline;
    private readonly IScene _scene;
    private readonly BenchmarkCamera _camera = new();
    private readonly bool _directionalShadows;
    private readonly bool _pointShadows;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ITextureFactory _textures;
    private readonly IModelFactory _models;
    private readonly IVertexArrayFactory? _vertexArrays;
    private readonly IVertexBufferFactory? _vertexBuffers;
    private readonly IIndexBufferFactory? _indexBuffers;
    private readonly bool _clearModelCache;
    private readonly Action? _onDispose;
    private readonly ExpectedShadow _expected;
    private readonly IScenePresenter _display;
    private readonly ShadowQuality? _editorShadowQuality;
    private readonly float _viewportHeightPixels;
    private bool _disposed;
    private Statistics _last = new();

    public PipelineBenchmarkRun(
        string resultName,
        float durationSeconds,
        IScene scene,
        bool directionalShadows,
        bool pointShadows,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory textures,
        IModelFactory models,
        IVertexArrayFactory? vertexArrays,
        IVertexBufferFactory? vertexBuffers,
        IIndexBufferFactory? indexBuffers,
        bool clearModelCache,
        ExpectedShadow expected,
        Action? onDispose = null,
        IScenePresenter? display = null,
        ShadowQuality? editorShadowQuality = null,
        IFrameBufferFactory? frameBuffers = null,
        float viewportHeightPixels = 0f)
    {
        _pipeline = new(graphics2D, graphics3D, textures, models, vertexArrays, vertexBuffers, indexBuffers, frameBuffers);
        ResultName = resultName;
        DurationSeconds = durationSeconds;
        _scene = scene;
        _directionalShadows = directionalShadows;
        _pointShadows = pointShadows;
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        _textures = textures;
        _models = models;
        _vertexArrays = vertexArrays;
        _vertexBuffers = vertexBuffers;
        _indexBuffers = indexBuffers;
        _clearModelCache = clearModelCache;
        _onDispose = onDispose;
        _expected = expected;
        _display = display ?? DirectScenePresenter.Instance;
        _editorShadowQuality = editorShadowQuality;
        _viewportHeightPixels = viewportHeightPixels;
    }

    public override string ResultName { get; }
    public override float DurationSeconds { get; }

    public override void Tick(TimeSpan delta) => _display.Present(DrawFrame);

    void DrawFrame()
    {
        if (_editorShadowQuality is { } quality)
            _graphics3D.SetShadowQuality(quality);

        _graphics2D.SetClearColor(_scene.BackgroundColor);
        _graphics2D.Clear();
        _graphics3D.ResetStats();
        _graphics3D.SetSkybox(string.IsNullOrWhiteSpace(_scene.Skybox) ? null : _scene.Skybox);
        _scene.UpdateWorldTransforms();
        _pipeline.RenderScene(_scene.Context, _camera.CreateView(_directionalShadows, _pointShadows, _viewportHeightPixels));
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

        if (_expected == ExpectedShadow.Directional && !_last.DirectionalShadow)
            result.CustomMetrics["Shadow pass"] = "skipped";
        if (_expected == ExpectedShadow.Point && _last.PointShadowLights == 0)
            result.CustomMetrics["Shadow pass"] = "skipped";
    }

    public override void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pipeline.Dispose();
        _onDispose?.Invoke();
        _scene.Dispose();
        if (_clearModelCache)
            _models.Clear();
    }

    public void Frame(Vector3 eye, Vector3 focal) => _camera.LookFrom(eye, focal);

    public void HandleInput(InputEvent windowEvent, bool pointerOverUi)
    {
        switch (windowEvent)
        {
            case MouseMovedEvent move:
                if (!pointerOverUi || _camera.Dragging)
                    _camera.Move(new Vector2(move.X, move.Y));
                break;
            case MouseButtonPressedEvent press when !pointerOverUi:
                _camera.Button(press.Button, true);
                break;
            case MouseButtonReleasedEvent release:
                _camera.Button(release.Button, false);
                break;
            case MouseScrolledEvent scroll when !pointerOverUi:
                _camera.Zoom(scroll.YOffset);
                break;
        }
    }
}
