using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Serilog;

namespace Engine.Renderer.Models;

internal class ModelFactory : IModelFactory
{
    private static readonly ILogger Logger = Log.ForContext<ModelFactory>();

    private readonly Func<string, (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph)> _import;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;
    private readonly Dictionary<string, Model?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public ModelFactory(
        AssimpModelImporter importer,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
        : this(importer.Import, vertexArrayFactory, vertexBufferFactory, indexBufferFactory)
    {
    }

    internal ModelFactory(
        Func<string, (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph)> import,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
    {
        _import = import;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;
    }

    public Model? Create(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;
        }

        var model = TryLoadModel(normalizedPath);

        lock (_cacheLock)
        {
            _cache[normalizedPath] = model;
        }

        return model;
    }

    private Model? TryLoadModel(string normalizedPath)
    {
        if (!File.Exists(normalizedPath))
        {
            Logger.Warning("Model file not found: {Path}", normalizedPath);
            return null;
        }

        try
        {
            var (submeshes, sceneGraph) = _import(normalizedPath);
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
                    submesh.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
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

    public void Clear()
    {
        lock (_cacheLock)
        {
            foreach (var model in _cache.Values)
                model?.Dispose();
            _cache.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
