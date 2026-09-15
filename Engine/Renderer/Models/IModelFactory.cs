using System.Diagnostics.CodeAnalysis;

namespace Engine.Renderer.Models;

public interface IModelFactory : IDisposable
{
    Model? Create(string path);
    bool TryGet(string path, [NotNullWhen(true)] out Model? model);
    void Clear();
}
