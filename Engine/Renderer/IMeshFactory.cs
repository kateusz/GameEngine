namespace Engine.Renderer;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    Mesh CreateFullscreenTriangle();
    void Clear();
}
