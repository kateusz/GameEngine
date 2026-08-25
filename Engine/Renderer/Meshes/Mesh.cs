using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

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
        int EntityId = -1)
    {
        public Vertex() : this(default, default, default, default, default) { }

        public static BufferLayout Layout { get; } = new([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float3, "a_Tangent"),
            new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);
    }

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }
    public Texture2D? DiffuseTexture { get; set; }
    public Texture2D? SpecularTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }
    public float Shininess { get; set; } = 32.0f;
    public Aabb? LocalAabb { get; private set; }

    public bool HasDiffuseMap => DiffuseTexture != null;
    public bool HasSpecularMap => SpecularTexture != null;
    public bool HasNormalMap => NormalTexture != null;

    private IVertexArray _vertexArray;
    private bool _initialized;
    private bool _disposed;

    public IVertexArray GetVertexArray()
    {
        return !_initialized
            ? throw new InvalidOperationException(
                $"Mesh '{Name}' not initialized. Call Initialize() before accessing vertex array.")
            : _vertexArray;
    }

    public Mesh(string name = "Unnamed")
    {
        Name = name;
        Vertices = [];
        Indices = [];
    }

    public void Initialize(IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
    {
        ArgumentNullException.ThrowIfNull(vertexArrayFactory);
        ArgumentNullException.ThrowIfNull(vertexBufferFactory);
        ArgumentNullException.ThrowIfNull(indexBufferFactory);

        if (_initialized)
            throw new InvalidOperationException(
                $"Mesh '{Name}' already initialized. Initialize() should only be called once.");

        _vertexArray = vertexArrayFactory.Create();
        var vertexBuffer = vertexBufferFactory.Create(Vertices);
        vertexBuffer.SetLayout(Vertex.Layout);
        _vertexArray.AddVertexBuffer(vertexBuffer);

        var indexBuffer = indexBufferFactory.Create([.. Indices], Indices.Count);
        _vertexArray.SetIndexBuffer(indexBuffer);

        LocalAabb = Vertices.Count == 0 ? null : ComputeLocalAabb(Vertices);
        _initialized = true;

        Vertices.Clear();
        Vertices.TrimExcess();
        Indices.Clear();
        Indices.TrimExcess();
    }

    public void Bind()
    {
        if (!_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' not initialized. Call Initialize() before binding.");

        _vertexArray.Bind();
    }

    public void Unbind()
    {
        _vertexArray.Unbind();
    }

    public int GetIndexCount() => _initialized ? _vertexArray.IndexBuffer.Count : Indices.Count;

    private static Aabb ComputeLocalAabb(List<Vertex> vertices)
    {
        var min = vertices[0].Position;
        var max = min;
        for (var i = 1; i < vertices.Count; i++)
        {
            min = Vector3.Min(min, vertices[i].Position);
            max = Vector3.Max(max, vertices[i].Position);
        }

        return new Aabb(min, max);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _vertexArray?.Dispose();

        _disposed = true;
    }

#if DEBUG
    ~Mesh()
    {
        if (!_disposed && _initialized)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MESH LEAK: Mesh '{Name}' not disposed! " +
                $"Indices: {GetIndexCount()}"
            );
        }

        Dispose(false);
    }
#endif
}