using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Meshes;

internal sealed class MeshFactory(
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IMeshFactory
{
    private Mesh? _cubeMesh;
    private bool _disposed;

    internal static readonly (Mesh.Vertex[] Vertices, uint[] Indices) CubeTemplate = CreateCubeTemplate();

    public Mesh CreateCube()
    {
        if (_cubeMesh != null)
            return _cubeMesh;

        var mesh = new Mesh("Cube");
        mesh.DiffuseTexture = textureFactory.GetWhiteTexture();
        mesh.Vertices.AddRange(CubeTemplate.Vertices);
        mesh.Indices.AddRange(CubeTemplate.Indices);
        mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        _cubeMesh = mesh;
        return mesh;
    }

    private static (Mesh.Vertex[] Vertices, uint[] Indices) CreateCubeTemplate()
    {
        var size = Aabb.UnitCubeHalfExtent;

        var tangentX = Vector3.UnitX;
        var tangentNegX = -Vector3.UnitX;
        var bitangentY = Vector3.UnitY;
        var bitangentNegZ = -Vector3.UnitZ;
        var bitangentZ = Vector3.UnitZ;

        var vertices = new Mesh.Vertex[24];
        vertices[0] = new Mesh.Vertex(new Vector3(-size, -size, size), Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentX, bitangentY);
        vertices[1] = new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentX, bitangentY);
        vertices[2] = new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentX, bitangentY);
        vertices[3] = new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentX, bitangentY);

        vertices[4] = new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitZ, new Vector2(1.0f, 0.0f), tangentNegX, bitangentY);
        vertices[5] = new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitZ, new Vector2(1.0f, 1.0f), tangentNegX, bitangentY);
        vertices[6] = new Mesh.Vertex(new Vector3(size, size, -size), -Vector3.UnitZ, new Vector2(0.0f, 1.0f), tangentNegX, bitangentY);
        vertices[7] = new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitZ, new Vector2(0.0f, 0.0f), tangentNegX, bitangentY);

        vertices[8] = new Mesh.Vertex(new Vector3(-size, size, -size), Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentNegZ);
        vertices[9] = new Mesh.Vertex(new Vector3(-size, size, size), Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentNegZ);
        vertices[10] = new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentNegZ);
        vertices[11] = new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentNegZ);

        vertices[12] = new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitY, new Vector2(0.0f, 1.0f), tangentX, bitangentZ);
        vertices[13] = new Mesh.Vertex(new Vector3(size, -size, -size), -Vector3.UnitY, new Vector2(1.0f, 1.0f), tangentX, bitangentZ);
        vertices[14] = new Mesh.Vertex(new Vector3(size, -size, size), -Vector3.UnitY, new Vector2(1.0f, 0.0f), tangentX, bitangentZ);
        vertices[15] = new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitY, new Vector2(0.0f, 0.0f), tangentX, bitangentZ);

        vertices[16] = new Mesh.Vertex(new Vector3(size, -size, -size), Vector3.UnitX, new Vector2(0.0f, 0.0f), -Vector3.UnitZ, bitangentY);
        vertices[17] = new Mesh.Vertex(new Vector3(size, size, -size), Vector3.UnitX, new Vector2(0.0f, 1.0f), -Vector3.UnitZ, bitangentY);
        vertices[18] = new Mesh.Vertex(new Vector3(size, size, size), Vector3.UnitX, new Vector2(1.0f, 1.0f), -Vector3.UnitZ, bitangentY);
        vertices[19] = new Mesh.Vertex(new Vector3(size, -size, size), Vector3.UnitX, new Vector2(1.0f, 0.0f), -Vector3.UnitZ, bitangentY);

        vertices[20] = new Mesh.Vertex(new Vector3(-size, -size, -size), -Vector3.UnitX, new Vector2(1.0f, 0.0f), Vector3.UnitZ, bitangentY);
        vertices[21] = new Mesh.Vertex(new Vector3(-size, -size, size), -Vector3.UnitX, new Vector2(0.0f, 0.0f), Vector3.UnitZ, bitangentY);
        vertices[22] = new Mesh.Vertex(new Vector3(-size, size, size), -Vector3.UnitX, new Vector2(0.0f, 1.0f), Vector3.UnitZ, bitangentY);
        vertices[23] = new Mesh.Vertex(new Vector3(-size, size, -size), -Vector3.UnitX, new Vector2(1.0f, 1.0f), Vector3.UnitZ, bitangentY);

        uint[] indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        ];

        return (vertices, indices);
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
