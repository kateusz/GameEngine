using System.Diagnostics;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

/// <summary>
/// Represents a loaded 3D model containing meshes and textures.
/// </summary>
public sealed class Model : IModel
{
    public string Directory { get; internal set; } = string.Empty;
    public List<Mesh> Meshes { get; } = [];

    internal List<Texture2D> LoadedTextures { get; } = [];

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var mesh in Meshes)
        {
            mesh?.Dispose();
        }
        Meshes.Clear();

        foreach (var texture in LoadedTextures)
        {
            texture?.Dispose();
        }
        LoadedTextures.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~Model()
    {
        if (!_disposed)
        {
            Debug.WriteLine(
                $"MODEL LEAK: Model '{Directory}' not disposed! " +
                $"Meshes: {Meshes.Count}, Textures: {LoadedTextures.Count}"
            );
        }
    }
#endif
}
