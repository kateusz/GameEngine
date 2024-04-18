namespace Engine.Platform.OpenTK;

public class OpenTKContext
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