namespace Engine.Renderer;

public interface IGraphicsContext
{
    void Init(Action swapBuffer);
    void SwapBuffers();
}