using Audio;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Input;
using Serilog;

namespace Engine.Core;

public abstract class Application : IApplication
{
    private static readonly ILogger Logger = Log.ForContext<Application>();

    private readonly IGameWindow _gameWindow;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly IFrameCompositor? _frameCompositor;
    private readonly IAudio _audio;
    private readonly IMeshFactory _meshFactory;
    private readonly IKeyboardInput? _keyboardInput;
    private IInputSystem? _inputSystem;
    private readonly List<ILayer> _layersStack = [];

    private const double MaxDeltaTime = 0.25; // 250ms = 4 FPS minimum

    protected Application(
        IGameWindow gameWindow,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        IAudio audio,
        IMeshFactory meshFactory,
        IFrameCompositor? frameCompositor = null,
        ILayer? inputOverlay = null,
        IKeyboardInput? keyboardInput = null)
    {
        _gameWindow = gameWindow;
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        _audio = audio;
        _meshFactory = meshFactory;
        _frameCompositor = frameCompositor;
        _keyboardInput = keyboardInput;

        _gameWindow.OnWindowEvent += HandleWindowEvent;
        _gameWindow.OnInputEvent += HandleInputEvent;
        _gameWindow.OnClose += HandleGameWindowClose;
        _gameWindow.OnUpdate += HandleUpdate;
        _gameWindow.OnWindowLoad += HandleGameWindowOnLoad;

        if (inputOverlay != null)
            PushOverlay(inputOverlay);
    }

    /// <summary>
    /// Initializes core engine subsystems and attaches all registered layers.
    /// </summary>
    /// <remarks>
    /// INITIALIZATION OWNERSHIP: Application is responsible for initializing all core
    /// graphics and audio subsystems (Graphics2D, Graphics3D, AudioEngine). Layers should
    /// NOT call Init() on these subsystems - they are guaranteed to be initialized before
    /// layer.OnAttach() is called. This prevents double initialization and ensures consistent
    /// resource management across all application types (Editor, Runtime, Sandbox).
    /// </remarks>
    private void HandleGameWindowOnLoad(IInputSystem inputSystem)
    {
        _graphics2D.Init();
        _graphics3D.Init();
        _audio.Initialize();

        _inputSystem = inputSystem;

        foreach (var layer in _layersStack)
            layer.OnAttach(inputSystem);
    }

    public void Run()
    {
        _gameWindow.Run();
    }

    public void PushLayer(ILayer layer)
    {
        _layersStack.Insert(0, layer);
    }

    public void PushOverlay(ILayer overlay)
    {
        _layersStack.Add(overlay);
    }

    public void PopOverlay(ILayer overlay)
    {
        if (_layersStack.Remove(overlay))
            SafeDetachLayer(overlay);
    }

    public void PopLayer(ILayer layer)
    {
        if (_layersStack.Remove(layer))
            SafeDetachLayer(layer);
    }

    private static void SafeDetachLayer(ILayer layer)
    {
        try
        {
            layer.OnDetach();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error detaching layer {layer.GetType().Name}");
        }
    }

    private void HandleUpdate(double platformDeltaTime)
    {
        var deltaTime = System.Math.Clamp(platformDeltaTime, 0.0, MaxDeltaTime);

        if (System.Math.Abs(deltaTime - platformDeltaTime) > double.Epsilon && platformDeltaTime > MaxDeltaTime)
        {
            Logger.Warning("Frame spike detected: {DeltaMs:F2}ms, clamping to {MaxDeltaMs}ms",
                platformDeltaTime * 1000, MaxDeltaTime * 1000);
        }

        var elapsed = TimeSpan.FromSeconds(deltaTime);

        _inputSystem?.Update(elapsed);

        for (var index = _layersStack.Count - 1; index >= 0; index--)
            _layersStack[index].OnUpdate(elapsed);

        _audio.Update(elapsed);

        _frameCompositor?.BeginFrame(elapsed);

        for (var index = _layersStack.Count - 1; index >= 0; index--)
            _layersStack[index].Draw();

        _frameCompositor?.EndFrame();

        _keyboardInput?.EndFrame();
    }

    private void HandleWindowEvent(WindowEvent @event)
    {
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].HandleWindowEvent(@event);
            if (@event.IsHandled)
                break;
        }
    }

    private void HandleInputEvent(InputEvent windowEvent)
    {
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].HandleInputEvent(windowEvent);
            if (windowEvent.IsHandled)
                break;
        }
    }

    private void HandleGameWindowClose(WindowCloseEvent @event)
    {
        for (var index = _layersStack.Count - 1; index >= 0; index--)
            SafeDetachLayer(_layersStack[index]);

        _layersStack.Clear();
        _graphics2D?.Dispose();
        _graphics3D?.Dispose();
        _audio.Dispose();
        _meshFactory.Clear();
    }
}
