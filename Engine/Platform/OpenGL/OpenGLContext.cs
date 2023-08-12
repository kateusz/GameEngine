using Engine.Events;
using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine.Platform.OpenGL;

public class OpenGLContext : IGraphicsContext
{
    private readonly IWindow _window;

    public OpenGLContext(IWindow window)
    {
        _window = window;
    }

    public event Action<Event> OnEvent;

    public void Init()
    {
    }

    public void SwapBuffers()
    {
       
    }
}