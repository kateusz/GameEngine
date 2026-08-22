using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models;

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(string path, IReadOnlyList<Mesh> submeshes)
    {
        Path = path;
        Submeshes = submeshes;
    }

    public string Path { get; }
    public IReadOnlyList<Mesh> Submeshes { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var submesh in Submeshes)
            submesh.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}