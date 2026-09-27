namespace Engine.Renderer.Models;

public interface IModelFactory : IDisposable
{
    Model? Create(string path, byte[]? runtimeMeshBytes = null);
    void Clear();
}
