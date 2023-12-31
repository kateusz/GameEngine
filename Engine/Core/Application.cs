using Engine.Core.Window;
using Engine.Events;
using NLog;

namespace Engine.Core;

public interface IApplication
{
    void Run();
    void PushLayer(Layer layer);
    void PushOverlay(Layer overlay);
}

public class Application : IApplication
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly IWindow _window;
    private readonly List<Layer> _layersStack = new();
    private bool _isRunning;
    private DateTime _lastTime;

    protected Application()
    {
        var windowProps = new WindowProps("Sandbox Engine testing!", 1280, 1024);

        _window = WindowFactory.Create(windowProps);
        _window.OnEvent += HandleOnEvent;
        _window.OnClose += HandleOnWindowClose;
        _window.OnUpdate += HandleOnUpdate;
        _isRunning = true;
        
        Renderer.Renderer.Instance.Init();
    }

    public void Run()
    {
        _window.Run();
    }

    public void PushLayer(Layer layer)
    {
        _layersStack.Insert(0, layer);
        layer.OnAttach();
    }

    public void PushOverlay(Layer overlay)
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
    
    private void HandleOnWindowClose(WindowCloseEvent @event)
    {
        _isRunning = false;
    }
}