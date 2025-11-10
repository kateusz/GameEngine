namespace Engine.Renderer;

/// <summary>
/// Interface for a 3D model loaded from file.
/// Contains meshes and textures loaded using the Assimp library.
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
