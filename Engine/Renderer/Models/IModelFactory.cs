namespace Engine.Renderer.Models;

public interface IModelFactory : IDisposable
{
    Model? Create(string path, byte[]? runtimeMeshBytes = null);

    /// <summary>Cache lookup only — never loads or uploads. Used by parallel opaque collect.</summary>
    bool TryGetCached(string path, out Model? model);

    void Clear();
}
