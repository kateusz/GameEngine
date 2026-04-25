using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Serilog;

namespace Engine.Renderer;

internal sealed class MeshFactory(
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory,
    FbxModelLoader fbxModelLoader) : IMeshFactory
{
    private readonly ILogger _logger = Log.ForContext<MeshFactory>();
    private readonly Dictionary<string, Mesh> _loadedMeshes = new();
    private readonly Dictionary<string, (List<Mesh> Meshes, List<PbrMaterial> Materials, List<ModelLightData> Lights)> _loadedModels = new();
    private bool _disposed;

    public Mesh CreateCube(ITextureFactory textureFactory, IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        var mesh = new Mesh("Cube", textureFactory);
        var size = 0.5f;

        var tangentX = Vector3.UnitX;
        var tangentNegX = -Vector3.UnitX;
        var bitangentY = Vector3.UnitY;
        var bitangentNegZ = -Vector3.UnitZ;
        var bitangentZ = Vector3.UnitZ;

        // Front face (+Z): Normal=(0,0,1), Tangent=(1,0,0), Bitangent=(0,1,0)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentX, bitangentY));

        // Back face (-Z): Normal=(0,0,-1), Tangent=(-1,0,0), Bitangent=(0,1,0)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), -Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentNegX, bitangentY));

        // Top face (+Y): Normal=(0,1,0), Tangent=(1,0,0), Bitangent=(0,0,-1)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentNegZ));

        // Bottom face (-Y): Normal=(0,-1,0), Tangent=(1,0,0), Bitangent=(0,0,1)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), -Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentZ));

        // Right face (+X): Normal=(1,0,0), Tangent=(0,0,-1), Bitangent=(0,1,0)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), Vector3.UnitX, new Vector2(0.0f, 0.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitX, new Vector2(0.0f, 1.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitX, new Vector2(1.0f, 1.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitX, new Vector2(1.0f, 0.0f), -Vector3.UnitZ, bitangentY));

        // Left face (-X): Normal=(-1,0,0), Tangent=(0,0,1), Bitangent=(0,1,0)
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitX, new Vector2(1.0f, 0.0f), Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitX, new Vector2(0.0f, 0.0f), Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), -Vector3.UnitX, new Vector2(0.0f, 1.0f), Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitX, new Vector2(1.0f, 1.0f), Vector3.UnitZ, bitangentY));

        mesh.Indices.AddRange([0, 1, 2, 2, 3, 0]);
        mesh.Indices.AddRange([4, 5, 6, 6, 7, 4]);
        mesh.Indices.AddRange([8, 9, 10, 10, 11, 8]);
        mesh.Indices.AddRange([12, 13, 14, 14, 15, 12]);
        mesh.Indices.AddRange([16, 17, 18, 18, 19, 16]);
        mesh.Indices.AddRange([20, 21, 22, 22, 23, 20]);

        mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        return mesh;
    }

    public Mesh CreateCube()
    {
        return CreateCube(textureFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
    }

    public (List<Mesh> Meshes, List<PbrMaterial> Materials, List<ModelLightData> Lights) LoadModel(string path)
    {
        if (_loadedModels.TryGetValue(path, out var cached))
            return cached;

        var result = fbxModelLoader.Load(path);

        // Filter out empty meshes that have no geometry (helper/dummy nodes in FBX)
        var validMeshes = new List<Mesh>();
        var validMaterials = new List<PbrMaterial>();

        for (var i = 0; i < result.Meshes.Count; i++)
        {
            var mesh = result.Meshes[i];
            if (mesh.Vertices.Count == 0 || mesh.Indices.Count == 0)
            {
                _logger.Debug("Skipping empty mesh '{Name}' (vertices: {V}, indices: {I})",
                    mesh.Name, mesh.Vertices.Count, mesh.Indices.Count);
                continue;
            }

            mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
            validMeshes.Add(mesh);
            validMaterials.Add(result.Materials[i]);
        }

        _logger.Information("Initialized {Count}/{Total} meshes from {Path}",
            validMeshes.Count, result.Meshes.Count, path);

        var entry = (validMeshes, validMaterials, result.Lights);
        _loadedModels[path] = entry;
        return entry;
    }

    public void Clear()
    {
        foreach (var (meshes, _, _) in _loadedModels.Values)
        {
            foreach (var mesh in meshes)
                mesh.Dispose();
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
                $"Cached meshes: {_loadedMeshes.Count}"
            );
        }
    }
#endif
}
