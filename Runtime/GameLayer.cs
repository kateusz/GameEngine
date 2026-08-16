using System.Numerics;
using ECS.Systems;
using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Input;
using Scripting;
using Serilog;

namespace Runtime;

public class GameLayer(
    IGraphics2D graphics2D,
    ISceneContext sceneContext,
    SceneFactory sceneFactory,
    ISceneSerializer sceneSerializer,
    IScriptEngine scriptEngine,
    IKeyboardInput keyboardInput,
    IMouseInput mouseInput,
    IPointerSurface pointerSurface,
    IGameWindow gameWindow,
    IFrameBufferFactory frameBufferFactory,
    PostProcessOrchestrator postProcessOrchestrator,
    GameConfiguration gameConfig,
    Func<IEnumerable<IGameSystem>> resolveGameSystems)
    : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    private readonly Action<IScene> _sceneChangedHandler = _ => Logger.Information("Active scene changed");
    private IFrameBuffer? _hdrFrameBuffer;

    public void OnAttach(IInputSystem inputSystem)
    {
        sceneContext.SceneChanged += _sceneChangedHandler;
        postProcessOrchestrator.Initialize();
        _hdrFrameBuffer = frameBufferFactory.Create();

        Logger.Information("Game layer attached.");

        var startupScenePath = Path.Combine(AppContext.BaseDirectory, gameConfig.StartupScenePath);

        if (!File.Exists(startupScenePath))
        {
            throw new InvalidOperationException(
                $"Startup scene not found: {startupScenePath} (base directory: {AppContext.BaseDirectory})");
        }

        Logger.Information("Loading startup scene from: {Path}", startupScenePath);

        var scene = sceneFactory.Create(startupScenePath, Path.GetFileNameWithoutExtension(startupScenePath));
        try
        {
            sceneSerializer.Deserialize(scene, startupScenePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load startup scene: {startupScenePath}", ex);
        }

        sceneContext.SetScene(scene);
        RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());

        var size = gameWindow.ClientSize;
        ResizeHdrBuffer((uint)size.X, (uint)size.Y);
        scene.OnViewportResize((uint)size.X, (uint)size.Y);
        Logger.Information("Startup scene loaded successfully");
    }

    public void OnDetach()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;

        Logger.Information("Game layer detached.");

        sceneContext.ActiveScene?.OnRuntimeStop();
        sceneContext.ActiveScene?.Dispose();
        _hdrFrameBuffer?.Dispose();
        _hdrFrameBuffer = null;
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (sceneContext.ActiveScene is not { } scene || _hdrFrameBuffer is null)
            return;

        pointerSurface.Set(Vector2.Zero, gameWindow.ClientSize);

        _hdrFrameBuffer.Bind();
        graphics2D.SetClearColor(scene.BackgroundColor);
        graphics2D.Clear();
        _hdrFrameBuffer.ClearAttachment(1, -1);
        scene.OnUpdateRuntime(timeSpan);
        _hdrFrameBuffer.Unbind();

        var spec = _hdrFrameBuffer.GetSpecification();
        postProcessOrchestrator.Run(
            _hdrFrameBuffer.GetColorAttachmentRendererId(),
            spec.Width,
            spec.Height,
            PostProcessSettings.FromScene(scene.PostProcess),
            tonemapTarget: null);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        if (keyboardInput is KeyboardInputState keyboardState)
            keyboardState.Apply(windowEvent);

        if (mouseInput is MouseInputState mouseState)
            mouseState.Apply(windowEvent);

        if (sceneContext is { ActiveScene: { } scene, ActiveScriptRuntimeStore: { } store })
            scriptEngine.ProcessEvent(windowEvent, scene.Context, store);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent)
        {
            Logger.Information("GameLayer: Window resized: {Width}x{Height}", resizeEvent.Width, resizeEvent.Height);
            ResizeHdrBuffer((uint)resizeEvent.Width, (uint)resizeEvent.Height);
            sceneContext.ActiveScene?.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw() { }

    private void ResizeHdrBuffer(uint width, uint height)
    {
        if (_hdrFrameBuffer is null || width == 0 || height == 0)
            return;

        var spec = _hdrFrameBuffer.GetSpecification();
        if (spec.Width == width && spec.Height == height)
            return;

        _hdrFrameBuffer.Resize(width, height);
    }
}
