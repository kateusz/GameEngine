using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;

namespace Engine.Renderer.Meshes;

public class Mesh : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct Vertex(
        Vector3 Position,
        Vector3 Normal,
        Vector2 TexCoord,
        Vector3 Tangent,
        Vector3 Bitangent,
        float BoneId0,
        float BoneId1,
        float BoneId2,
        float BoneId3,
        Vector4 Weights)
    {
        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, Vector3 tangent, Vector3 bitangent)
            : this(position, normal, texCoord, tangent, bitangent, -1f, -1f, -1f, -1f, default)
        {
        }

        // 14 floats + 4 bone ids + 4 weights. Must match CreateVertexLayout stride.
        public static int GetSize() => sizeof(float) * 22;
    }

    // Must match Vertex packing exactly — a larger stride causes vertex explosion.
    // Entity ID is a per-draw uniform (u_EntityID), not a mesh vertex attribute.
    // Bone ids are floats: macOS integer vertex attribs (ivec4 / IPointer) read as 0 or garbage.
    internal static BufferLayout CreateVertexLayout() => new([
        new BufferElement(ShaderDataType.Float3, "a_Position"),
        new BufferElement(ShaderDataType.Float3, "a_Normal"),
        new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
        new BufferElement(ShaderDataType.Float3, "a_Tangent"),
        new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
        new BufferElement(ShaderDataType.Float4, "a_BoneIndexF"),
        new BufferElement(ShaderDataType.Float4, "a_Weights")
    ]);

    public string Name { get; set; }

    private List<Vertex> _vertices = [];
    private List<uint> _indices = [];
    private int _indexCount;

    public List<Vertex> Vertices
    {
        get
        {
            EnsureCpuDataAccessible();
            return _vertices;
        }
        set
        {
            EnsureCpuDataAccessible();
            _vertices = value ?? [];
        }
    }

    public List<uint> Indices
    {
        get
        {
            EnsureCpuDataAccessible();
            return _indices;
        }
        set
        {
            EnsureCpuDataAccessible();
            _indices = value ?? [];
        }
    }

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
    }

    public void Initialize(IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        if (_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' already initialized. Initialize() should only be called once.");

        _vertexArray = vertexArrayFactory.Create();
        _vertexBuffer = vertexBufferFactory.Create((uint)(_vertices.Count * Vertex.GetSize()));

        _vertexBuffer.SetLayout(CreateVertexLayout());
        _vertexArray.AddVertexBuffer(_vertexBuffer);
        _vertexBuffer.SetMeshData(_vertices, _vertices.Count * Vertex.GetSize());

        _indexCount = _indices.Count;
        _indexBuffer = indexBufferFactory.Create(_indices.ToArray(), _indexCount);
        _vertexArray.SetIndexBuffer(_indexBuffer);

        _vertices.Clear();
        _indices.Clear();
        _vertices.TrimExcess();
        _indices.TrimExcess();

        _initialized = true;
    }

    public void Bind()
    {
        if (!_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' not initialized. Call Initialize() before binding.");

        _vertexArray.Bind();
    }

    public void Unbind() => _vertexArray.Unbind();

    public int GetIndexCount() => _initialized ? _indexCount : _indices.Count;

    private void EnsureCpuDataAccessible()
    {
        if (_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' CPU data was released after GPU upload.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _vertexArray?.Dispose();
        _vertexBuffer = null!;
        _indexBuffer = null!;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
