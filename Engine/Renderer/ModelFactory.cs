using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

internal sealed class ModelFactory : IModelFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<ModelFactory>();

    private readonly ITextureFactory _textureFactory;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;
    private readonly Assimp _assimp;
    private readonly AssimpModelImporter _importer;
    private readonly Dictionary<string, Model> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public ModelFactory(
        ITextureFactory textureFactory,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
        : this(textureFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory, Assimp.GetApi())
    {
    }

    internal ModelFactory(
        ITextureFactory textureFactory,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory,
        Assimp assimp)
    {
        _textureFactory = textureFactory;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;
        _assimp = assimp;
        _importer = new AssimpModelImporter(assimp);
    }

    public Model? Create(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;
        }

        if (!System.IO.File.Exists(normalizedPath))
        {
            Logger.Warning("Model file not found: {Path}", normalizedPath);
            return null;
        }

        try
        {
            var submeshes = _importer.Import(normalizedPath);
            if (submeshes.Count == 0)
            {
                Logger.Warning("Model has no meshes: {Path}", normalizedPath);
                return null;
            }

            var initialized = new List<ModelSubmesh>(submeshes.Count);
            foreach (var submesh in submeshes)
            {
                try
                {
                    ResolveMaterialTextures(submesh.Material);
                    submesh.Mesh.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
                    initialized.Add(submesh);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to initialize mesh '{MeshName}' in {Path}", submesh.Mesh.Name, normalizedPath);
                    submesh.Mesh.Dispose();
                }
            }

            if (initialized.Count == 0)
            {
                Logger.Warning("No submeshes initialized for model: {Path}", normalizedPath);
                return null;
            }

            var model = new Model(normalizedPath, initialized);

            lock (_cacheLock)
            {
                _cache[normalizedPath] = model;
            }

            return model;
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
                model.Dispose();
            _cache.Clear();
        }

        _assimp.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResolveMaterialTextures(MeshMaterial material)
    {
        material.AlbedoTexture = LoadTexture(material.AlbedoTexturePath, sRgb: true);
        material.MetallicRoughnessTexture = LoadTexture(material.MetallicRoughnessTexturePath);
        material.NormalTexture = LoadTexture(material.NormalTexturePath);
    }

    private Texture2D? LoadTexture(string? path, bool sRgb = false)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return _textureFactory.Create(path, sRgb);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", path);
            return null;
        }
    }
}
