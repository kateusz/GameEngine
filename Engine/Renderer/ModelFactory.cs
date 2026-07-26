using Engine.Core;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer;

/// <summary>
/// Loads GPU-ready models from cooked <c>.mesh</c> files only.
/// Raw interchange (.fbx/.glb/.gltf) must be cooked first — never loaded here.
/// Vertex upload uses Mesh StaticDraw layout (incl. bone attrs); pose is shader uniforms only — never mutate cached VBOs.
/// </summary>
internal sealed class ModelFactory : IModelFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<ModelFactory>();

    private readonly ITextureFactory _textureFactory;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;
    private readonly Dictionary<string, Model> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public ModelFactory(
        ITextureFactory textureFactory,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
    {
        _textureFactory = textureFactory;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;
    }

    public void Evict(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (_cacheLock)
        {
            if (_cache.Remove(normalizedPath, out var model))
                model.Dispose();
        }
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            foreach (var model in _cache.Values)
                model.Dispose();
            _cache.Clear();
        }
    }

    public Model? Create(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;

            if (!File.Exists(normalizedPath))
            {
                Logger.Warning("Model file not found: {Path}", normalizedPath);
                return null;
            }

            if (!IsMeshExtension(normalizedPath))
            {
                Logger.Warning(
                    "Rejected non-.mesh model path (cook required): {Path} extension={Extension}",
                    normalizedPath, Path.GetExtension(normalizedPath));
                return null;
            }

            try
            {
                Model model;
                using (var stream = File.OpenRead(normalizedPath))
                    model = MeshReader.Read(stream);

                if (model.Submeshes.Count == 0)
                {
                    Logger.Warning("Model has no meshes: {Path}", normalizedPath);
                    return null;
                }

                var initialized = new List<ModelSubmesh>(model.Submeshes.Count);
                foreach (var submesh in model.Submeshes)
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

                var result = new Model(initialized);
                _cache[normalizedPath] = result;
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load model: {Path}", normalizedPath);
                return null;
            }
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

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static bool IsMeshExtension(string path) =>
        Path.GetExtension(path).Equals(".mesh", StringComparison.OrdinalIgnoreCase);

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
            var resolved = PathBuilder.Resolve(path);
            if (!PathBuilder.IsUnderAssets(resolved))
            {
                Logger.Warning("Rejected texture path outside assets root: {Path} → {Resolved}", path, resolved);
                return null;
            }

            return _textureFactory.Create(resolved, sRgb);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", path);
            return null;
        }
    }
}
