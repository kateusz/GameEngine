namespace Engine.Renderer;

/// <summary>
/// Interface for a loaded 3D model containing meshes and textures.
/// </summary>
public interface IModel : IDisposable
{
    /// <summary>
    /// Gets the directory path of the model file.
    /// </summary>
    string Directory { get; }

    /// <summary>
    /// Gets the list of meshes that make up this model.
    /// </summary>
    List<Mesh> Meshes { get; }
}
