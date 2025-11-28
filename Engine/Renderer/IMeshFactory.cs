using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

/// <summary>
/// Factory interface for creating and caching mesh resources.
/// </summary>
public interface IMeshFactory
{
    /// <summary>
    /// Creates or retrieves a cached mesh from an OBJ file.
    /// </summary>
    /// <param name="objFilePath">Path to the OBJ file.</param>
    /// <returns>A mesh instance from the model file.</returns>
    Mesh Create(string objFilePath);

    /// <summary>
    /// Creates a procedural cube mesh.
    /// </summary>
    /// <param name="textureFactory">Factory for creating textures</param>
    /// <param name="vertexArrayFactory">Factory for creating vertex arrays</param>
    /// <param name="vertexBufferFactory">Factory for creating vertex buffers</param>
    /// <param name="indexBufferFactory">Factory for creating index buffers</param>
    /// <returns>A new cube mesh.</returns>
    Mesh CreateCube(ITextureFactory textureFactory, IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory);

    /// <summary>
    /// Clears all cached meshes and disposes loaded models to free GPU resources.
    /// Should be called when shutting down or when clearing the asset cache.
    /// </summary>
    void Clear();
}
