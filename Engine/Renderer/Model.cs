namespace Engine.Renderer;

public sealed record ModelSubmesh(Mesh Mesh, MeshMaterial Material);

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(
        IReadOnlyList<ModelSubmesh> submeshes,
        IReadOnlyList<SkeletonBone>? bones = null,
        IReadOnlyList<AnimationClip>? clips = null)
    {
        Submeshes = submeshes;
        Bones = bones ?? [];
        Clips = clips ?? [];
    }

    public IReadOnlyList<ModelSubmesh> Submeshes { get; }
    public IReadOnlyList<SkeletonBone> Bones { get; }
    public IReadOnlyList<AnimationClip> Clips { get; }
    public bool HasSkeleton => Bones.Count > 0;

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
