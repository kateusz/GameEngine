using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine.Platform.OpenGL;

public class OpenGLContext : IGraphicsContext
{
    private Action? _swapBuffer;

    public void Init(Action swapBuffer)
    {
        _swapBuffer = swapBuffer;
    }

    public void SwapBuffers()
    {
        _swapBuffer?.Invoke();
    }
}