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

    private readonly Func<string, byte[]?, (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph)> _import;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;
    private readonly Dictionary<string, Model?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public ModelFactory(
        ITextureFactory textureFactory,
        EngineHostOptions hostOptions,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
        : this(
            (path, meshBytes) => LoadFromRuntimeMesh(path, textureFactory, hostOptions.EnsureRuntimeMeshSiblingOnLoad,
                meshBytes),
            vertexArrayFactory,
            vertexBufferFactory,
            indexBufferFactory)
    {
    }

    internal ModelFactory(
        Func<string, byte[]?, (IReadOnlyList<Mesh> Submeshes, ModelSceneNode? SceneGraph)> import,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
    {
        _import = import;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;
    }

    public Model? Create(string path, byte[]? runtimeMeshBytes = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;
        }

        var model = TryLoadModel(normalizedPath, runtimeMeshBytes);

        lock (_cacheLock)
        {
            _cache[normalizedPath] = model;
        }

        return model;
    }

    private Model? TryLoadModel(string normalizedPath, byte[]? runtimeMeshBytes)
    {
        try
        {
            var (submeshes, sceneGraph) = _import(normalizedPath, runtimeMeshBytes);
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
        ITextureFactory textureFactory,
        bool ensureRuntimeMeshSibling,
        byte[]? runtimeMeshBytes)
    {
        if (!RuntimeMeshPaths.IsModelSourceExtension(normalizedPath))
        {
            Logger.Warning("Unsupported model source extension: {Path}", normalizedPath);
            return ([], null);
        }

        if (ensureRuntimeMeshSibling)
        {
            if (!File.Exists(normalizedPath))
            {
                Logger.Warning("Model source file not found: {Path}", normalizedPath);
                return ([], null);
            }

            if (!RuntimeMeshBuilder.TryEnsureUpToDate(normalizedPath))
                return ([], null);
        }

        byte[] bytes;
        if (runtimeMeshBytes != null)
        {
            bytes = runtimeMeshBytes;
        }
        else
        {
            var siblingPath = RuntimeMeshPaths.SiblingPath(normalizedPath);
            if (!File.Exists(siblingPath))
            {
                Logger.Warning("Runtime mesh file not found: {Path}", siblingPath);
                return ([], null);
            }

            bytes = File.ReadAllBytes(siblingPath);
        }

        if (!RuntimeMeshReader.TryRead(bytes, out var source) || source == null)
        {
            Logger.Warning("Failed to read runtime mesh: {Path}", normalizedPath);
            return ([], null);
        }

        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(normalizedPath)) ?? string.Empty;
        var meshes = new List<Mesh>(source.Submeshes.Count);
        foreach (var submesh in source.Submeshes)
        {
            var mesh = new Mesh(submesh.Name);
            mesh.Vertices.AddRange(submesh.Vertices);
            mesh.Indices.AddRange(submesh.Indices);
            mesh.MetallicFactor = submesh.Metallic;
            mesh.RoughnessFactor = submesh.Roughness;
            mesh.BaseColorFactor = submesh.BaseColorFactor;
            mesh.AlphaCutout = submesh.AlphaCutout;
            mesh.DoubleSided = submesh.DoubleSided;
            mesh.AlphaCutoff = submesh.AlphaCutoff;
            Logger.Debug(
                "PBR set for mesh={Mesh} model={Path}: metallic={Metallic} roughness={Roughness} baseColor={BaseColor} " +
                "maps albedo={Albedo} normal={Normal} metallicRoughness={Mr} occlusion={Occlusion} " +
                "hasAlbedo={HasAlbedo} hasNormal={HasNormal} hasMr={HasMr} hasOcclusion={HasOcclusion}",
                submesh.Name,
                normalizedPath,
                submesh.Metallic,
                submesh.Roughness,
                submesh.BaseColorFactor,
                submesh.DiffusePath,
                submesh.NormalPath,
                submesh.MetallicRoughnessPath,
                submesh.OcclusionPath,
                !string.IsNullOrEmpty(submesh.DiffusePath),
                !string.IsNullOrEmpty(submesh.NormalPath),
                !string.IsNullOrEmpty(submesh.MetallicRoughnessPath),
                !string.IsNullOrEmpty(submesh.OcclusionPath));
            RuntimeMeshTextureRefs.BindTextures(mesh, submesh, sourceDirectory, textureFactory);
            meshes.Add(mesh);
        }

        var sceneGraph = RuntimeMeshSceneGraph.Unflatten(source.Nodes, source.Lights);
        return (meshes, sceneGraph);
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
