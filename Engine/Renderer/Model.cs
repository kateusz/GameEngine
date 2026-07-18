namespace Engine.Renderer;

public sealed record ModelSubmesh(Mesh Mesh, MeshMaterial Material);

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(IReadOnlyList<ModelSubmesh> submeshes) => Submeshes = submeshes;

    public IReadOnlyList<ModelSubmesh> Submeshes { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var submesh in Submeshes)
            submesh.Mesh.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
