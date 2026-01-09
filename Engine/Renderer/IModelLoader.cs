namespace Engine.Renderer;

/// <summary>
/// Interface for loading 3D models from files.
/// </summary>
public interface IModelLoader
{
    /// <summary>
    /// Loads a 3D model from the specified file path.
    /// </summary>
    /// <param name="path">Path to the model file (.obj, .fbx, .gltf, etc.)</param>
    /// <returns>The loaded model containing meshes and textures.</returns>
    IModel Load(string path);

    /// <summary>
    /// Gets the supported file extensions for this loader.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}
