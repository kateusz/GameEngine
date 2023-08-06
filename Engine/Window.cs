using Engine.Events;
using Engine.Platform.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine;

public record WindowProps(string Title, int Width, int Height);

public interface IWindow
{
    void Run();
    event Action<Event> OnEvent;
    void OnUpdate();
}

public class Window : IWindow
{
    private IGraphicsContext _context;


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
}