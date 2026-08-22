namespace Engine.Renderer.Meshes;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    void Clear();
}