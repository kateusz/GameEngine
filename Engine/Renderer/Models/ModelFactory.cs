using System.Diagnostics.CodeAnalysis;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Serilog;

namespace Engine.Renderer.Models;

internal class ModelFactory : IModelFactory
{
    private static readonly ILogger Logger = Log.ForContext<ModelFactory>();

    private readonly Func<string, (IReadOnlyList<Mesh> Submeshes, IReadOnlyList<MeshMaterial> Materials, ModelSceneNode? SceneGraph)> _import;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
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
        Func<string, (IReadOnlyList<Mesh> Submeshes, IReadOnlyList<MeshMaterial> Materials, ModelSceneNode? SceneGraph)> import,
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
            var existed = File.Exists(normalizedPath);
            var mtime = existed ? File.GetLastWriteTimeUtc(normalizedPath) : DateTime.MinValue;

            if (_cache.TryGetValue(normalizedPath, out var cached) &&
                cached.Existed == existed &&
                cached.MtimeUtc == mtime)
                return cached.Model;

            cached?.Model?.Dispose();

            Model? model = null;
            if (existed)
                model = TryLoadModel(normalizedPath);

            _cache[normalizedPath] = new CacheEntry(model, mtime, existed);
            return model;
        }
    }

    public bool TryGet(string path, [NotNullWhen(true)] out Model? model)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = Path.GetFullPath(path);
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached) && cached.Model != null)
            {
                model = cached.Model;
                return true;
            }
        }

        model = null;
        return false;
    }

    private Model? TryLoadModel(string normalizedPath)
    {
        try
        {
            var (submeshes, materials, sceneGraph) = _import(normalizedPath);
            if (submeshes.Count == 0)
            {
                Logger.Warning("Model has no meshes: {Path}", normalizedPath);
                return null;
            }

            if (submeshes.Count != materials.Count)
            {
                Logger.Warning("Model material count mismatch: {Path}", normalizedPath);
                DisposeMeshes(submeshes);
                return null;
            }

            var initialized = new List<Mesh>(submeshes.Count);
            try
            {
                foreach (var submesh in submeshes)
                {
                    submesh.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
                    initialized.Add(submesh);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to initialize model meshes: {Path}", normalizedPath);
                DisposeMeshes(submeshes);
                return null;
            }

            return new Model(normalizedPath, initialized, materials, sceneGraph);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load model: {Path}", normalizedPath);
            return null;
        }
    }

    private static void DisposeMeshes(IEnumerable<Mesh> meshes)
    {
        foreach (var mesh in meshes)
            mesh.Dispose();
    }

    public void Clear()
    {
        lock (_cacheLock)
        {
            foreach (var entry in _cache.Values)
                entry.Model?.Dispose();
            _cache.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Clear();
        _disposed = true;
    }

    private sealed record CacheEntry(Model? Model, DateTime MtimeUtc, bool Existed);
}
