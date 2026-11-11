using System.Numerics;
using Engine.Events.Input;
using Engine.Renderer;
using Engine.Renderer.Buffers;
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
    private readonly IScene _scene;
    private readonly BenchmarkCamera _camera = new();
    private readonly bool _directionalShadows;
    private readonly bool _pointShadows;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ITextureFactory _textures;
    private readonly IModelFactory _models;
    private readonly IVertexArrayFactory _vertexArrays;
    private readonly IVertexBufferFactory _vertexBuffers;
    private readonly IIndexBufferFactory _indexBuffers;
    private readonly bool _clearModelCache;
    private readonly Action? _onDispose;
    private readonly ExpectedShadow _expected;
    private readonly IScenePresenter _display;
    private readonly bool _matchEditorLight;
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
        IVertexArrayFactory vertexArrays,
        IVertexBufferFactory vertexBuffers,
        IIndexBufferFactory indexBuffers,
        bool clearModelCache,
        ExpectedShadow expected,
        Action? onDispose = null,
        IScenePresenter? display = null,
        bool matchEditorLight = false)
    {
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
        _matchEditorLight = matchEditorLight;
    }

    public override string ResultName { get; }
    public override float DurationSeconds { get; }

    public override void Tick(TimeSpan delta) => _display.Present(DrawFrame);

    void DrawFrame()
    {
        if (_matchEditorLight)
            _graphics3D.SetShadowQuality(ShadowQuality.For(ShadowPreset.Low)); // ponytail: stock editor defaults; ignores editor-preferences.json

        _graphics2D.SetClearColor(_scene.BackgroundColor);
        _graphics2D.Clear();
        _graphics3D.ResetStats();
        _graphics3D.SetSkybox(string.IsNullOrWhiteSpace(_scene.Skybox) ? null : _scene.Skybox);
        _scene.UpdateWorldTransforms();
        SceneRenderPipeline.RenderScene(
            _scene.Context, _graphics2D, _graphics3D, _textures, _models,
            _camera.CreateView(_directionalShadows, _pointShadows),
            _vertexArrays, _vertexBuffers, _indexBuffers);
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
        _scene.Dispose();
        if (_clearModelCache)
            _models.Clear();
        _onDispose?.Invoke();
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
