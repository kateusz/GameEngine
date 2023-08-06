using Engine.Events;
using Engine.Platform.OpenGL;
using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine;

public record WindowProps(string Title, int Width, int Height);

public interface IWindow
{
    void Run();
    event Action<Event> OnEvent;
    void OnUpdate();
    event Action<WindowCloseEvent> OnClose;
}

public class Window : IWindow
{
    private readonly IGraphicsContext _context;

    public Window(WindowProps props)
    {
        _context = new OpenGLContext(props);
    }

    public void Run()
    {
        _context.Init();
    }

    public event Action<Event> OnEvent;

    public void OnUpdate()
    {
    }

    public event Action<WindowCloseEvent> OnClose;
}