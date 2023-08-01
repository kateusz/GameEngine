using Engine.Events;
using NLog;

namespace Engine;

public interface IApplication
{
    void Run();
    event Action<Event> OnEvent;
    event Func<WindowCloseEvent, bool> OnWindowClose;

    void PushLayer(Layer layer);
    void PushOverlay(Layer overlay);
}

public class Application : IApplication
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly IWindow _window;
    private readonly List<Layer> _layersStack = new();
    private bool _isRunning;

    public event Action<Event>? OnEvent;
    public event Func<WindowCloseEvent, bool>? OnWindowClose;

    protected Application()
    {
        var windowProps = new WindowProps("Sandbox Engine testing!", 1280, 720, HandleOnEvent);

        _window = new Window(windowProps);
        _isRunning = true;

        OnEvent += HandleOnEvent;
        OnWindowClose += HandleOnWindowClose;
    }


    public void Run()
    {
        _window.Run();
        // while (_isRunning)
        // {
        //     for (var index = _layersStack.Count - 1; index >= 0; index--)
        //     {
        //         _layersStack[index].OnEvent();
        //     }
        //
        //     //_window.OnUpdate();
        // }
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


    private bool HandleOnWindowClose(WindowCloseEvent @event)
    {
        _isRunning = false;
        return true;
    }
}