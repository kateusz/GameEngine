using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer.Meshes;

internal sealed class MeshFactory(
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IMeshFactory
{
    private readonly ILogger _logger = Log.ForContext<MeshFactory>();
    private Mesh? _cubeMesh;
    private bool _disposed;

    public Mesh CreateCube()
    {
        if (_cubeMesh != null)
            return _cubeMesh;

        var mesh = new Mesh("Cube", textureFactory);
        const float size = 0.5f;

        var tangentX = Vector3.UnitX;
        var tangentNegX = -Vector3.UnitX;
        var bitangentY = Vector3.UnitY;
        var bitangentNegZ = -Vector3.UnitZ;
        var bitangentZ = Vector3.UnitZ;

        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentX, bitangentY));

        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), -Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentNegX, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentNegX, bitangentY));

        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, -size), Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentNegZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentNegZ));

        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), -Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentZ));

        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, -size), Vector3.UnitX, new Vector2(0.0f, 0.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitX, new Vector2(0.0f, 1.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitX, new Vector2(1.0f, 1.0f), -Vector3.UnitZ, bitangentY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitX, new Vector2(1.0f, 0.0f), -Vector3.UnitZ, bitangentY));

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
        _cubeMesh = mesh;
        return mesh;
    }

    public void Clear()
    {
        _cubeMesh?.Dispose();
        _cubeMesh = null;
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
}
