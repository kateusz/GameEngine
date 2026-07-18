using Engine.Renderer.Animation;

namespace Engine.Renderer;

public sealed record ModelSubmesh(Mesh Mesh, MeshMaterial Material);

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(
        string path,
        IReadOnlyList<ModelSubmesh> submeshes,
        Skeleton? skeleton = null,
        IReadOnlyList<AnimationClip>? clips = null)
    {
        Path = path;
        Submeshes = submeshes;
        Skeleton = skeleton;
        Clips = clips ?? Array.Empty<AnimationClip>();
    }

    public string Path { get; }
    public IReadOnlyList<ModelSubmesh> Submeshes { get; }
    public Skeleton? Skeleton { get; }
    public IReadOnlyList<AnimationClip> Clips { get; }
    public bool HasSkeleton => Skeleton is { IsEmpty: false };

    public AnimationClip? FindClip(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var clip in Clips)
        {
            if (string.Equals(clip.Name, name, StringComparison.OrdinalIgnoreCase))
                return clip;
        }

        return null;
    }

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
