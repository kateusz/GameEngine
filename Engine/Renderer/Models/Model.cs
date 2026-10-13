using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models;

public sealed class Model : IDisposable
{
    private bool _disposed;

    public Model(
        string path,
        IReadOnlyList<Mesh> submeshes,
        ModelSceneNode? sceneGraph = null,
        SkinData? skin = null,
        Matrix4x4[]? restPose = null)
    {
        Path = path;
        Submeshes = submeshes;
        SceneGraph = sceneGraph;
        Skin = skin;
        RestPose = restPose ?? [];
    }

    public string Path { get; }
    public IReadOnlyList<Mesh> Submeshes { get; }
    public ModelSceneNode? SceneGraph { get; }
    public SkinData? Skin { get; }
    public Matrix4x4[] RestPose { get; }

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