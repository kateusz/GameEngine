using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Platform.SilkNet.Audio;
using Engine.Renderer;
using NLog;

namespace Engine.Core;

public interface IApplication
{
    void Run();
    void PushLayer(ILayer layer);
    void PushOverlay(ILayer overlay);
}

public class Application : IApplication
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static ImGuiLayer ImGuiLayer;

    private readonly IGameWindow _gameWindow;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly List<ILayer> _layersStack = new();
    private bool _isRunning;
    private DateTime _lastTime;

    protected Application(IGameWindow gameWindow, IGraphics2D graphics2D, IGraphics3D graphics3D, bool enableImGui = false)
    {
        _gameWindow = gameWindow;
        _gameWindow.OnEvent += HandleOnEvent;
        _gameWindow.OnClose += HandleOnGameWindowClose;
        _gameWindow.OnUpdate += HandleOnUpdate;
        _gameWindow.OnWindowLoad += HandleGameWindowOnLoad;
        
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        
        _isRunning = true;
        
        InputState.Init();

        if (enableImGui)
        {
            ImGuiLayer = new ImGuiLayer("ImGUI");
            PushOverlay(ImGuiLayer);
        }
    }

    private void HandleGameWindowOnLoad()
    {
        _graphics2D.Init();
        _graphics3D.Init();
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

        ImGuiLayer?.Begin(elapsed);
        
        for (var index = _layersStack.Count - 1; index >= 0; index--)
        {
            _layersStack[index].OnImGuiRender();
        }
        
        ImGuiLayer?.End();
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