using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Serilog;

namespace Engine.Renderer.Meshes;

internal sealed class MeshFactory(
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IMeshFactory
{
    private readonly ILogger _logger = Log.ForContext<MeshFactory>();
    private Mesh? _cubeMesh;
    private Mesh? _sphereMesh;
    private bool _disposed;

    public Mesh CreateCube()
    {
        if (_cubeMesh != null)
            return _cubeMesh;

        var mesh = new Mesh("Cube");
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

    public Mesh CreateSphere()
    {
        if (_sphereMesh != null)
            return _sphereMesh;

        var mesh = new Mesh("Sphere");
        const int segments = 32;
        const int rings = 16;
        const float radius = 0.5f;

        for (var ring = 0; ring <= rings; ring++)
        {
            var theta = MathF.PI * ring / rings;
            var (sinT, cosT) = MathF.SinCos(theta);
            for (var seg = 0; seg <= segments; seg++)
            {
                var phi = 2f * MathF.PI * seg / segments;
                var (sinP, cosP) = MathF.SinCos(phi);
                var normal = new Vector3(sinT * cosP, cosT, sinT * sinP);
                var tangent = new Vector3(-sinP, 0f, cosP);
                var bitangent = Vector3.Cross(normal, tangent);
                mesh.Vertices.Add(new Mesh.Vertex(normal * radius, normal,
                    new Vector2((float)seg / segments, 1f - (float)ring / rings), tangent, bitangent));
            }
        }

        for (var ring = 0; ring < rings; ring++)
        {
            for (var seg = 0; seg < segments; seg++)
            {
                var a = ring * (segments + 1) + seg;
                var b = a + segments + 1;
                mesh.Indices.AddRange([(uint)a, (uint)b, (uint)(a + 1), (uint)(a + 1), (uint)b, (uint)(b + 1)]);
            }
        }

        mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        _sphereMesh = mesh;
        return mesh;
    }

    public void Clear()
    {
        _cubeMesh?.Dispose();
        _cubeMesh = null;
        _sphereMesh?.Dispose();
        _sphereMesh = null;
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
