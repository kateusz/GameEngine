using Engine.Audio;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.ImGuiNet;
using Engine.Renderer;
using Serilog;

namespace Engine.Core;

public abstract class Application : IApplication
{
    private static readonly ILogger Logger = Log.ForContext<Application>();

    private readonly IGameWindow _gameWindow;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly IImGuiLayer? _imGuiLayer;
    private readonly IAudioEngine _audioEngine;
    private IInputSystem? _inputSystem;
    private readonly List<ILayer> _layersStack = [];

    private bool _isRunning;
    private const double MaxDeltaTime = 0.25; // 250ms = 4 FPS minimum

    protected Application(IGameWindow gameWindow, IGraphics2D graphics2D,  IGraphics3D graphics3D, IAudioEngine audioEngine, IImGuiLayer? imGuiLayer = null)
    {
        _gameWindow = gameWindow ?? throw new ArgumentNullException(nameof(gameWindow));
        _graphics2D = graphics2D ?? throw new ArgumentNullException(nameof(graphics2D));
        _graphics3D = graphics3D;
        _audioEngine = audioEngine;

        _gameWindow.OnWindowEvent += HandleWindowEvent;
        _gameWindow.OnInputEvent += HandleInputEvent;
        _gameWindow.OnClose += HandleGameWindowClose;
        _gameWindow.OnUpdate += HandleUpdate;
        _gameWindow.OnWindowLoad += HandleGameWindowOnLoad;
        _isRunning = true;

        if (imGuiLayer != null)
        {
            _imGuiLayer = imGuiLayer;
            PushOverlay(imGuiLayer);
        }
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
        // Initialize core graphics and audio subsystems - owned by Application
        _graphics2D.Init();
        _graphics3D.Init();
        _audioEngine.Initialize();

        _inputSystem = inputSystem;

        // Attach all layers - graphics subsystems are already initialized at this point
        foreach (var layer in _layersStack)
        {
            // TODO: there should be better way to pass input system only for ImGuiLayer...
            layer.OnAttach(inputSystem);
        }
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
        {
            SafeDetachLayer(overlay);
        }
    }

    public void PopLayer(ILayer layer)
    {
        if (_layersStack.Remove(layer))
        {
            SafeDetachLayer(layer);
        }
    }

    private void SafeDetachLayer(ILayer layer)
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
        // Clamp to reasonable range to protect against system sleep, debugger pauses, etc.
        var deltaTime = System.Math.Clamp(platformDeltaTime, 0.0, MaxDeltaTime);

        // Log warning if we had to clamp (indicates lag spike or system pause)
        if (System.Math.Abs(deltaTime - platformDeltaTime) > double.Epsilon && platformDeltaTime > MaxDeltaTime)
        {
            Logger.Warning("Frame spike detected: {DeltaMs:F2}ms, clamping to {MaxDeltaMs}ms",
                platformDeltaTime * 1000, MaxDeltaTime * 1000);
        }

        var elapsed = TimeSpan.FromSeconds(deltaTime);

        _inputSystem?.Update(elapsed);

        // LAYER ITERATION POLICY: Updates propagate in REVERSE order (overlays first, game layers last)
        // This ensures UI overlays update before the underlying game logic, allowing UI to:
        // - Capture and handle input before game logic processes it
        // - Update visual state based on the most recent frame
        // - Render on top of game content with correct state
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnUpdate(elapsed);
        }

        _imGuiLayer?.Begin(elapsed);

        // ImGui rendering also uses reverse order to maintain consistent layer ordering
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].Draw();
        }

        _imGuiLayer?.End();
    }

    private void HandleWindowEvent(WindowEvent @event)
    {
        // LAYER ITERATION POLICY: Window events propagate in REVERSE order (overlays first, game layers last)
        // This ensures UI overlays handle window events (resize, focus, etc.) before game layers:
        // - UI can adjust layout and viewport before game logic processes the event
        // - Overlays can intercept and handle window events to prevent game logic from responding
        // - Consistent with input event and update iteration order
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].HandleWindowEvent(@event);
            if (@event.IsHandled)
                break;
        }
    }
    
    private void HandleInputEvent(InputEvent windowEvent)
    {
        // LAYER ITERATION POLICY: Input events propagate in REVERSE order (overlays first, game layers last)
        // This ensures UI overlays receive input events before game logic:
        // - UI buttons and controls can consume clicks before game logic processes them
        // - Menus and popups can block input from reaching the game when active
        // - Consistent with window event and update iteration order
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].HandleInputEvent(windowEvent);
            if (windowEvent.IsHandled)
                break;
        }
    }
    
    private void HandleGameWindowClose(WindowCloseEvent @event)
    {
        _isRunning = false;

        // Detach all layers in reverse order (LIFO) to ensure proper cleanup
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            SafeDetachLayer(_layersStack[index]);
        }

        _layersStack.Clear();

        // Shutdown audio engine
        _audioEngine.Shutdown();

        // Clear mesh factory cache and dispose all loaded models
        MeshFactory.Clear();
    }
}