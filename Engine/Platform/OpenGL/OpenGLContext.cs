namespace Engine.Platform.OpenGL;

public class OpenGLContext
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