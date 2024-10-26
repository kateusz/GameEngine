using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
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
    private readonly List<ILayer> _layersStack = new();
    private bool _isRunning;
    private DateTime _lastTime;

    protected Application(bool enableImGui = false)
    {
        var windowProps = new WindowProps("Sandbox Engine testing!", 1280, 720);

        _gameWindow = WindowFactory.Create(windowProps);
        _gameWindow.OnEvent += HandleOnEvent;
        _gameWindow.OnClose += HandleOnGameWindowClose;
        _gameWindow.OnUpdate += HandleOnUpdate;
        _gameWindow.OnWindowLoad += HandleGameWindowOnLoad;
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
        Renderer2D.Instance.Init();
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
        Logger.Debug(@event);

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