using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Platform.SilkNet.Audio;
using Engine.Renderer;
using NLog;

namespace Engine.Core;

public class Application : IApplication
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly IGameWindow _gameWindow;
    private readonly List<ILayer> _layersStack = [];
    private bool _isRunning;
    private DateTime _lastTime;
    private readonly IImGuiLayer _imGuiLayer;

    protected Application(IGameWindow gameWindow, IImGuiLayer imGuiLayer)
    {
        _gameWindow = gameWindow;
        _gameWindow.OnEvent += HandleOnEvent;
        _gameWindow.OnClose += HandleOnGameWindowClose;
        _gameWindow.OnUpdate += HandleOnUpdate;
        _gameWindow.OnWindowLoad += HandleGameWindowOnLoad;
        _isRunning = true;
        
        InputState.Init();
        
        _imGuiLayer = imGuiLayer;
        PushOverlay(imGuiLayer);
    }

    private void HandleGameWindowOnLoad()
    {
        Graphics2D.Instance.Init();
        Graphics3D.Instance.Init();
        AudioEngine.Instance.Initialize();
        
        foreach (var layer in _layersStack)
        {
            layer.OnAttach();
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
    
    private void HandleOnUpdate()
    {
        var currentTime = DateTime.Now;
        var elapsed = currentTime - _lastTime;
        _lastTime = currentTime;
        
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnUpdate(elapsed);
        }

        _imGuiLayer.Begin(elapsed);
        
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnImGuiRender();
        }
        
        _imGuiLayer.End();
    }

    private void HandleOnEvent(Event @event)
    {
        //Logger.Debug(@event);

        foreach (var layer in _layersStack)
        {
            layer.HandleEvent(@event);
            if (@event.IsHandled)
            {
                break;
            }
        }
    }
    
    private void HandleOnGameWindowClose(WindowCloseEvent @event)
    {
        _isRunning = false;
    }
}