namespace Engine.Renderer.Models;

public interface IModelFactory : IDisposable
{
    Model? Create(string path, byte[]? runtimeMeshBytes = null);
    bool IsCached(string path);
    void Clear();
}
