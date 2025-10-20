using System.Diagnostics;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.ImGuiNet;
using Engine.Platform.SilkNet.Audio;
using Engine.Renderer;
using Serilog;

namespace Engine.Core;

public abstract class Application : IApplication
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<Application>();

    private readonly IGameWindow _gameWindow;
    private readonly IImGuiLayer? _imGuiLayer;
    private IInputSystem? _inputSystem;
    private readonly List<ILayer> _layersStack = [];
    
    private bool _isRunning;
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private double _lastFrameTime = -1.0;
    private const double MaxDeltaTime = 0.25; // 250ms = 4 FPS minimum

    protected Application(IGameWindow gameWindow, IImGuiLayer? imGuiLayer = null)
    {
        _gameWindow = gameWindow;
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

    private void HandleGameWindowOnLoad(IInputSystem inputSystem)
    {
        Graphics2D.Instance.Init();
        Graphics3D.Instance.Init();
        AudioEngine.Instance.Initialize();
        
        _inputSystem = inputSystem;
        
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
    
    private void HandleUpdate()
    {
        double currentTime = _frameTimer.Elapsed.TotalSeconds;

        // First frame initialization - use zero delta to avoid massive spike
        double deltaTime;
        if (_lastFrameTime < 0)
        {
            _lastFrameTime = currentTime;
            deltaTime = 0.0; // First frame gets zero delta
        }
        else
        {
            deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;
        }

        // Clamp delta time to prevent "spiral of death" on lag spikes
        // Maximum 250ms (4 FPS) - anything longer is clamped
        if (deltaTime > MaxDeltaTime)
        {
            Logger.Warning("Frame spike detected: {DeltaMs:F2}ms, clamping to {MaxDeltaMs}ms", deltaTime * 1000, MaxDeltaTime * 1000);
            deltaTime = MaxDeltaTime;
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
            _layersStack[index].OnImGuiRender();
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
        // - Menus and dialogs can block input from reaching the game when active
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
    }
}