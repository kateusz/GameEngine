using Engine.Core.DI;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models.RuntimeMesh;
using Engine.Renderer.Textures;
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
        ITextureFactory textureFactory,
        EngineHostOptions hostOptions,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
        : this(
            path => LoadFromRuntimeMesh(path, importer, textureFactory, hostOptions.CookRuntimeMeshesFromSource),
            vertexArrayFactory,
            vertexBufferFactory,
            indexBufferFactory)
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

            long vertices = 0;
            long indices = 0;
            foreach (var submesh in initialized)
            {
                vertices += submesh.VertexCount;
                indices += submesh.GetIndexCount();
            }

            Logger.Information(
                "Loaded model {Path}: submeshes={Submeshes} vertices={Vertices} indices={Indices} triangles={Triangles}",
                normalizedPath, initialized.Count, vertices, indices, indices / 3);

            foreach (var submesh in initialized.OrderByDescending(m => m.VertexCount).Take(8))
            {
                var indexCount = submesh.GetIndexCount();
                Logger.Information(
                    "Model submesh vertices={Vertices} indices={Indices} triangles={Triangles} name={Name}",
                    submesh.VertexCount, indexCount, indexCount / 3, submesh.Name);
            }

            return new Model(normalizedPath, initialized, sceneGraph);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load model: {Path}", normalizedPath);
            return null;
        }
    }

    private static (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph) LoadFromRuntimeMesh(
        string normalizedPath,
        AssimpModelImporter importer,
        ITextureFactory textureFactory,
        bool cookFromSource)
    {
        if (!RuntimeMeshPaths.IsModelSourceExtension(normalizedPath))
        {
            Logger.Warning("Unsupported model source extension: {Path}", normalizedPath);
            return ([], null);
        }

        if (cookFromSource)
        {
            if (!File.Exists(normalizedPath))
            {
                Logger.Warning("Model source file not found: {Path}", normalizedPath);
                return ([], null);
            }

            if (!EnsureRuntimeMeshUpToDate(normalizedPath, importer))
                return ([], null);
        }

        var siblingPath = RuntimeMeshPaths.SiblingPath(normalizedPath);
        if (!File.Exists(siblingPath))
        {
            Logger.Warning("Runtime mesh file not found: {Path}", siblingPath);
            return ([], null);
        }

        var bytes = File.ReadAllBytes(siblingPath);
        if (!RuntimeMeshReader.TryRead(bytes, out var source) || source == null)
        {
            Logger.Warning("Failed to read runtime mesh: {Path}", siblingPath);
            return ([], null);
        }

        return SourceModelMaterializer.ToMeshes(source, normalizedPath, textureFactory);
    }

    private static bool EnsureRuntimeMeshUpToDate(string sourcePath, AssimpModelImporter importer)
    {
        var siblingPath = RuntimeMeshPaths.SiblingPath(sourcePath);
        var stamp = RuntimeMeshStamp.FromFile(sourcePath);
        if (File.Exists(siblingPath)
            && RuntimeMeshReader.TryReadStamp(File.ReadAllBytes(siblingPath), out var fileStamp)
            && fileStamp.Matches(stamp))
            return true;

        var imported = importer.ImportSource(sourcePath);
        if (imported == null || imported.Submeshes.Count == 0)
            return false;

        return RuntimeMeshWriter.TryWrite(siblingPath, imported, stamp);
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
    }
}
