using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Serilog;

namespace Engine.Renderer.Models;

internal class ModelFactory(AssimpModelImporter importer,
    IVertexArrayFactory vertexArrayFactory, 
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IModelFactory
{
    private static readonly ILogger Logger = Log.ForContext<ModelFactory>();
    
    private readonly Dictionary<string, Model?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;
    
    public Model? Create(string path, bool mergeByMaterial = false)
    {
        var normalizedPath = Path.GetFullPath(path);
        var cacheKey = BuildCacheKey(normalizedPath, mergeByMaterial);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var model = TryLoadModel(normalizedPath, mergeByMaterial);

        lock (_cacheLock)
        {
            _cache[cacheKey] = model;
        }

        return model;
    }

    private static string BuildCacheKey(string normalizedPath, bool mergeByMaterial) =>
        mergeByMaterial ? $"{normalizedPath}|m" : $"{normalizedPath}|r";

    private Model? TryLoadModel(string normalizedPath, bool mergeByMaterial)
    {
        if (!File.Exists(normalizedPath))
        {
            Logger.Warning("Model file not found: {Path}", normalizedPath);
            return null;
        }

        try
        {
            var (submeshes, sceneGraph) = importer.Import(normalizedPath, mergeByMaterial);
            if (submeshes.Count == 0)
            {
                Logger.Warning("Model has no meshes: {Path}", normalizedPath);
                return null;
            }

            var initialized = new List<Mesh>(submeshes.Count);
            foreach (var submesh in submeshes)
            {
                try
                {
                    submesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
                    initialized.Add(submesh);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to initialize mesh '{MeshName}' in {Path}", submesh.Name,
                        normalizedPath);
                    submesh.Dispose();
                }
            }

            if (initialized.Count == 0)
            {
                Logger.Warning("No submeshes initialized for model: {Path}", normalizedPath);
                return null;
            }

            return new Model(normalizedPath, initialized, sceneGraph);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load model: {Path}", normalizedPath);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_cacheLock)
        {
            foreach (var model in _cache.Values)
                if (model != null)
                    model.Dispose();
            _cache.Clear();
        }
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}