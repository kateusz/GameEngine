using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models;

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(
        string path,
        IReadOnlyList<Mesh> submeshes,
        IReadOnlyList<MeshMaterial> materials,
        ModelSceneNode? sceneGraph = null)
    {
        ArgumentNullException.ThrowIfNull(submeshes);
        ArgumentNullException.ThrowIfNull(materials);
        if (submeshes.Count != materials.Count)
            throw new ArgumentException("Materials must align 1:1 with submeshes.", nameof(materials));

        Path = path;
        Submeshes = submeshes;
        Materials = materials;
        SceneGraph = sceneGraph;
    }

    public string Path { get; }
    public IReadOnlyList<Mesh> Submeshes { get; }
    public IReadOnlyList<MeshMaterial> Materials { get; }
    public ModelSceneNode? SceneGraph { get; }

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
