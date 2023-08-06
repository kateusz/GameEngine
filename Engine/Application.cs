using Engine.Events;
using NLog;

namespace Engine;

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
    
    protected Application()
    {
        var windowProps = new WindowProps("Sandbox Engine testing!", 800, 600);

        _window = new Window(windowProps);
        _window.OnEvent += HandleOnEvent;
        _window.OnClose += HandleOnWindowClose;
        _isRunning = true;
    }


    public void Run()
    {
        _window.Run();
        while (_isRunning)
        {
            for (var index = _layersStack.Count - 1; index >= 0; index--)
            {
                _layersStack[index].OnUpdate();
            }

            _window.OnUpdate();
        }
    }

    public void PushLayer(Layer layer)
    {
        _layersStack.Insert(0, layer);
    }

    public void PushOverlay(Layer overlay)
    {
        _layersStack.Add(overlay);
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