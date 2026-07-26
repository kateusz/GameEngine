using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Buffers.VertexArray;

namespace Engine.Renderer;

public class Mesh : IDisposable
{
    public record struct Vertex(
        Vector3 Position,
        Vector3 Normal,
        Vector2 TexCoord,
        Vector3 Tangent,
        Vector3 Bitangent)
    {
        public static int GetSize() => sizeof(float) * (3 + 3 + 2 + 3 + 3); // 56 bytes
    }

    // Must match Vertex packing exactly — a larger stride causes vertex explosion.
    // Entity ID is a per-draw uniform (u_EntityID), not a mesh vertex attribute.
    internal static BufferLayout CreateVertexLayout() => new([
        new BufferElement(ShaderDataType.Float3, "a_Position"),
        new BufferElement(ShaderDataType.Float3, "a_Normal"),
        new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
        new BufferElement(ShaderDataType.Float3, "a_Tangent"),
        new BufferElement(ShaderDataType.Float3, "a_Bitangent")
    ]);

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }

    private IVertexArray _vertexArray = null!;
    private IVertexBuffer _vertexBuffer = null!;
    private IIndexBuffer _indexBuffer = null!;
    private bool _initialized;
    private bool _disposed;

    public IVertexArray GetVertexArray()
    {
        if (!_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' not initialized. Call Initialize() before accessing vertex array.");
        return _vertexArray;
    }

    public Mesh(string name = "Unnamed")
    {
        Name = name;
        Vertices = [];
        Indices = [];
    }

    public void Initialize(IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        if (_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' already initialized. Initialize() should only be called once.");

        _vertexArray = vertexArrayFactory.Create();
        _vertexBuffer = vertexBufferFactory.Create((uint)(Vertices.Count * Vertex.GetSize()));

        _vertexBuffer.SetLayout(CreateVertexLayout());
        _vertexArray.AddVertexBuffer(_vertexBuffer);
        _vertexBuffer.SetMeshData(Vertices, Vertices.Count * Vertex.GetSize());

        _indexBuffer = indexBufferFactory.Create(Indices.ToArray(), Indices.Count);
        _vertexArray.SetIndexBuffer(_indexBuffer);

        _initialized = true;
    }

    public void Bind()
    {
        if (!_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' not initialized. Call Initialize() before binding.");

        _vertexArray.Bind();
    }

    public void Unbind() => _vertexArray.Unbind();

    public int GetIndexCount() => Indices.Count;

    public void Dispose()
    {
        if (_disposed)
            return;

        _vertexArray?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
