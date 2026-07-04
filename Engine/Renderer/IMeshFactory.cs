namespace Engine.Renderer;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    void Clear();
}
