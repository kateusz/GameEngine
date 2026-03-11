namespace Engine.Renderer;

/// <summary>
/// Factory interface for creating and caching mesh resources.
/// </summary>
public interface IMeshFactory : IDisposable
{
    /// <summary>
    /// Creates or retrieves a cached mesh from an OBJ file.
    /// </summary>
    /// <param name="objFilePath">Path to the OBJ file.</param>
    /// <returns>A mesh instance from the model file.</returns>
    Mesh Create(string objFilePath);
    
    /// <summary>
    /// Loads a model file and returns all meshes with PBR materials.
    /// </summary>
    IModel CreateModel(string filePath);

    /// <summary>
    /// Clears all cached meshes and disposes loaded models to free GPU resources.
    /// Should be called when shutting down or when clearing the asset cache.
    /// </summary>
    void Clear();
}
