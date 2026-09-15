using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;

namespace Engine.Renderer.Meshes;

internal sealed class MeshFactory(
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IMeshFactory
{
    private Mesh? _cubeMesh;
    private bool _disposed;

    public Mesh CreateCube()
    {
        if (_cubeMesh != null)
            return _cubeMesh;

        const float size = 0.5f;
        var tangentX = Vector3.UnitX;
        var tangentNegX = -Vector3.UnitX;
        var bitangentY = Vector3.UnitY;
        var bitangentNegZ = -Vector3.UnitZ;
        var bitangentZ = Vector3.UnitZ;

        Mesh.Vertex[] vertices =
        [
            new(new Vector3(-size, -size, size), Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentX, bitangentY),
            new(new Vector3(size, -size, size), Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentX, bitangentY),
            new(new Vector3(size, size, size), Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentX, bitangentY),
            new(new Vector3(-size, size, size), Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentX, bitangentY),

            new(new Vector3(-size, -size, -size), -Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentNegX, bitangentY),
            new(new Vector3(-size, size, -size), -Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentNegX, bitangentY),
            new(new Vector3(size, size, -size), -Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentNegX, bitangentY),
            new(new Vector3(size, -size, -size), -Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentNegX, bitangentY),

            new(new Vector3(-size, size, -size), Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentNegZ),
            new(new Vector3(-size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentNegZ),
            new(new Vector3(size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentNegZ),
            new(new Vector3(size, size, -size), Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentNegZ),

            new(new Vector3(-size, -size, -size), -Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentZ),
            new(new Vector3(size, -size, -size), -Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentZ),
            new(new Vector3(size, -size, size), -Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentZ),
            new(new Vector3(-size, -size, size), -Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentZ),

            new(new Vector3(size, -size, -size), Vector3.UnitX, new Vector2(0.0f, 0.0f), -Vector3.UnitZ, bitangentY),
            new(new Vector3(size, size, -size), Vector3.UnitX, new Vector2(1.0f, 1.0f), -Vector3.UnitZ, bitangentY),
            new(new Vector3(size, size, size), Vector3.UnitX, new Vector2(1.0f, 1.0f), -Vector3.UnitZ, bitangentY),
            new(new Vector3(size, -size, size), Vector3.UnitX, new Vector2(1.0f, 0.0f), -Vector3.UnitZ, bitangentY),

            new(new Vector3(-size, -size, -size), -Vector3.UnitX, new Vector2(1.0f, 0.0f), Vector3.UnitZ, bitangentY),
            new(new Vector3(-size, -size, size), -Vector3.UnitX, new Vector2(0.0f, 0.0f), Vector3.UnitZ, bitangentY),
            new(new Vector3(-size, size, size), -Vector3.UnitX, new Vector2(0.0f, 1.0f), Vector3.UnitZ, bitangentY),
            new(new Vector3(-size, size, -size), -Vector3.UnitX, new Vector2(1.0f, 1.0f), Vector3.UnitZ, bitangentY)
        ];

        uint[] indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        ];

        _cubeMesh = Create("Cube", vertices, indices);
        return _cubeMesh;
    }

    public Mesh Create(string name, IReadOnlyList<Mesh.Vertex> vertices, IReadOnlyList<uint> indices)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        if (vertices.Count == 0)
            throw new ArgumentException("Mesh must have vertices.", nameof(vertices));

        var mesh = new Mesh(name);
        mesh.Vertices.AddRange(vertices);
        mesh.Indices.AddRange(indices);
        mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        return mesh;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cubeMesh?.Dispose();
        _cubeMesh = null;
        _disposed = true;
    }
}
