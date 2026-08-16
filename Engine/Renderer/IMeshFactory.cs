namespace Engine.Renderer;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    Mesh CreateSphere();
    void Clear();
}
