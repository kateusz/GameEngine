namespace Engine.Renderer.Meshes;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    Mesh CreateSphere();
    void Clear();
}
