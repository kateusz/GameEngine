using System.Numerics;
using ECS.Systems;
using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Engine.UI.Paper;
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
    PaperInputAdapter paperInputAdapter,
    PaperInputGate paperInputGate,
    IGameWindow gameWindow,
    GameConfiguration gameConfig,
    Func<IEnumerable<IGameSystem>> resolveGameSystems,
    IFrameBufferFactory frameBufferFactory,
    FxaaPass fxaaPass)
    : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    private readonly Action<IScene> _sceneChangedHandler = _ => Logger.Information("Active scene changed");
    private IFrameBuffer? _sceneFrameBuffer;

    public void OnAttach(IInputSystem inputSystem)
    {
        sceneContext.SceneChanged += _sceneChangedHandler;

        Logger.Information("Game layer attached.");

        var startupScenePath = Path.Combine(AppContext.BaseDirectory, gameConfig.StartupScenePath);

        if (!File.Exists(startupScenePath))
        {
            throw new InvalidOperationException(
                $"Startup scene not found: {startupScenePath} (base directory: {AppContext.BaseDirectory})");
        }

        Logger.Information("Loading startup scene from: {Path}", startupScenePath);

        var scene = sceneFactory.Create(
            startupScenePath,
            Path.GetFileNameWithoutExtension(startupScenePath),
            sceneSerializer.PeekDimension(startupScenePath));
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
        scene.OnViewportResize((uint)size.X, (uint)size.Y);
        fxaaPass.Init();
        TryCreateSceneFramebuffer();
        Logger.Information("Startup scene loaded successfully");
    }

    public void OnDetach()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;

        Logger.Information("Game layer detached.");

        sceneContext.ActiveScene?.OnRuntimeStop();
        sceneContext.ActiveScene?.Dispose();
        _sceneFrameBuffer?.Dispose();
        _sceneFrameBuffer = null;
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (sceneContext.ActiveScene is not { } scene)
            return;

        pointerSurface.Set(Vector2.Zero, gameWindow.ClientSize);

        var (fbWidth, fbHeight) = FramebufferPixelSize();
        if (fxaaPass.Available && _sceneFrameBuffer != null && fbWidth > 0 && fbHeight > 0)
        {
            EnsureSceneFramebufferSize(fbWidth, fbHeight);
            _sceneFrameBuffer.Bind();
            graphics2D.SetClearColor(scene.BackgroundColor);
            graphics2D.Clear();
            scene.OnUpdateRuntime(timeSpan);
            _sceneFrameBuffer.Unbind();
            fxaaPass.Apply(_sceneFrameBuffer.GetColorAttachmentRendererId(), fbWidth, fbHeight, dest: null);
        }
        else
        {
            graphics2D.SetClearColor(scene.BackgroundColor);
            graphics2D.Clear();
            scene.OnUpdateRuntime(timeSpan);
        }
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        paperInputAdapter.Apply(windowEvent);

        if (keyboardInput is KeyboardInputState keyboardState)
            keyboardState.Apply(windowEvent);

        if (mouseInput is MouseInputState mouseState)
            mouseState.Apply(windowEvent);

        if (PaperInputGate.Blocks(windowEvent, paperInputGate))
        {
            windowEvent.IsHandled = true;
            return;
        }

        if (sceneContext is { ActiveScene: { } scene, ActiveScriptRuntimeStore: { } store })
            scriptEngine.ProcessEvent(windowEvent, scene.Context, store);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent)
        {
            Logger.Information("GameLayer: Window resized: {Width}x{Height}", resizeEvent.Width, resizeEvent.Height);
            sceneContext.ActiveScene?.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
            if (resizeEvent.Width > 0 && resizeEvent.Height > 0)
                EnsureSceneFramebufferSize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw() { }

    private (uint Width, uint Height) FramebufferPixelSize()
    {
        var scale = gameWindow.ContentScale;
        var size = gameWindow.ClientSize;
        return ((uint)(size.X * scale), (uint)(size.Y * scale));
    }

    private void TryCreateSceneFramebuffer()
    {
        if (!fxaaPass.Available)
            return;

        var (width, height) = FramebufferPixelSize();
        if (width == 0 || height == 0)
            return;

        try
        {
            _sceneFrameBuffer = frameBufferFactory.Create(SceneColorDepthSpec(width, height));
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "FXAA scene framebuffer failed");
            _sceneFrameBuffer = null;
        }
    }

    private void EnsureSceneFramebufferSize(uint width, uint height)
    {
        if (_sceneFrameBuffer == null || width == 0 || height == 0)
            return;

        var spec = _sceneFrameBuffer.GetSpecification();
        if (spec.Width == width && spec.Height == height)
            return;

        _sceneFrameBuffer.Resize(width, height);
    }

    private static FrameBufferSpecification SceneColorDepthSpec(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                },
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth),
            ])
        };
}
