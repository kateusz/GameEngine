using System.Diagnostics;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.ImGuiNet;
using Engine.Platform.SilkNet.Audio;
using Engine.Renderer;
using NLog;

namespace Engine.Core;

public abstract class Application : IApplication
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly IGameWindow _gameWindow;
    private readonly IImGuiLayer? _imGuiLayer;
    private IInputSystem? _inputSystem;
    private readonly List<ILayer> _layersStack = [];
    
    private bool _isRunning;
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private double _lastFrameTime = -1.0;

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

        // First frame initialization - skip update to avoid massive delta
        if (_lastFrameTime < 0)
        {
            _lastFrameTime = currentTime;
            return;
        }

        double deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // Clamp delta time to prevent "spiral of death" on lag spikes
        // Maximum 250ms (4 FPS) - anything longer is clamped
        if (deltaTime > 0.25)
        {
            Logger.Warn($"Frame spike detected: {deltaTime * 1000:F2}ms, clamping to 250ms");
            deltaTime = 0.25;
        }

        var elapsed = TimeSpan.FromSeconds(deltaTime);

        _inputSystem?.Update(elapsed);
        
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnUpdate(elapsed);
        }

        _imGuiLayer?.Begin(elapsed);
        
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnImGuiRender();
        }
        
        _imGuiLayer?.End();
    }

    private void HandleWindowEvent(WindowEvent @event)
    {
        foreach (var layer in _layersStack)
        {
            layer.HandleWindowEvent(@event);
            if (@event.IsHandled)
                break;
        }
    }
    
    private void HandleInputEvent(InputEvent windowEvent)
    {
        // Input events handled in reverse order (overlay layers first)
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