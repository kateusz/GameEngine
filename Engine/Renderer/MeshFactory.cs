using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Serilog;

namespace Engine.Renderer;

/// <summary>
/// Factory for creating and caching mesh resources.
/// </summary>
internal sealed class MeshFactory(
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IMeshFactory
{
    private readonly ILogger _logger = Log.ForContext<MeshFactory>();
    private readonly Dictionary<string, Mesh> _loadedMeshes = new();
    private readonly Dictionary<string, Model> _loadedModels = new();
    private bool _disposed;

    /// <summary>
    /// Creates or retrieves a cached mesh from an OBJ file.
    /// </summary>
    /// <param name="objFilePath">Path to the OBJ file.</param>
    /// <returns>A mesh instance from the model file.</returns>
    public Mesh Create(string objFilePath)
    {
        if (_loadedMeshes.TryGetValue(objFilePath, out var existingMesh))
        {
            return existingMesh;
        }

        var model = new Model(objFilePath, textureFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        var mesh = model.Meshes.First();

        if (mesh.Vertices.Count > 50000 || mesh.Indices.Count > 100000)
        {
            _logger.Warning("Large mesh loaded from {ObjFilePath}: {VertexCount} vertices, {IndexCount} indices",
                objFilePath, mesh.Vertices.Count, mesh.Indices.Count);
        }

        _loadedMeshes[objFilePath] = mesh;
        _loadedModels[objFilePath] = model;
        return mesh;
    }

    public IModel CreateModel(string filePath)
    {
        if (_loadedModels.TryGetValue(filePath, out var existingModel))
        {
            return existingModel;
        }

        var model = new Model(filePath, textureFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);

        var totalVerts = model.Meshes.Sum(m => m.Vertices.Count);
        var totalIndices = model.Meshes.Sum(m => m.Indices.Count);
        _logger.Information("Model loaded from {FilePath}: {MeshCount} meshes, {VertexCount} vertices, {IndexCount} indices",
            filePath, model.Meshes.Count, totalVerts, totalIndices);

        _loadedModels[filePath] = model;
        foreach (var mesh in model.Meshes)
        {
            _loadedMeshes[$"{filePath}:{mesh.Name}"] = mesh;
        }

        return model;
    }
    
    /// <summary>
    /// Clears all cached meshes and disposes loaded models to free GPU resources.
    /// Should be called when shutting down or when clearing the asset cache.
    /// </summary>
    public void Clear()
    {
        // Dispose all loaded models (which will dispose their meshes and textures)
        foreach (var model in _loadedModels.Values)
        {
            model?.Dispose();
        }

        _loadedModels.Clear();
        _loadedMeshes.Clear();

        _logger.Information("MeshFactory cache cleared and resources disposed");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.Debug("Disposing MeshFactory and clearing cache");

        Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~MeshFactory()
    {
        if (!_disposed)
        {
            System.Diagnostics.Debug.WriteLine(
                $"FACTORY LEAK: MeshFactory not disposed! " +
                $"Cached meshes: {_loadedMeshes.Count}, models: {_loadedModels.Count}"
            );
        }
    }
#endif
}
