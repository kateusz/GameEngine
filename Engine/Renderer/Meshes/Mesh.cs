using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Meshes;

public class Mesh : IDisposable
{
    public record struct Vertex(
        Vector3 Position,
        Vector3 Normal,
        Vector2 TexCoord,
        Vector3 Tangent,
        Vector3 Bitangent,
        int EntityId = -1)
    {
        public static int GetSize() => sizeof(float) * (3 + 3 + 2 + 3 + 3) + sizeof(int); // 60 bytes
    }

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }
    public Texture2D? DiffuseTexture { get; set; }
    public Texture2D? SpecularTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }
    public float Shininess { get; set; } = 32.0f;
    public List<Texture2D> Textures { get; set; }
    public Matrix4x4 NodeTransform { get; set; } = Matrix4x4.Identity;
    
    public bool HasDiffuseMap => DiffuseTexture != null;
    public bool HasSpecularMap => SpecularTexture != null;
    public bool HasNormalMap => NormalTexture != null;

    private IVertexArray _vertexArray;
    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
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
        Textures = [];
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
        _vertexBuffer = vertexBufferFactory.Create((uint)(Vertices.Count * Vertex.GetSize()));

        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float3, "a_Tangent"),
            new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);

        _vertexBuffer.SetLayout(layout);
        _vertexArray.AddVertexBuffer(_vertexBuffer);

        _vertexBuffer.SetMeshData(Vertices);

        _indexBuffer = indexBufferFactory.Create([.. Indices], Indices.Count);
        _vertexArray.SetIndexBuffer(_indexBuffer);

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

    public int GetIndexCount() => _initialized ? _indexBuffer.Count : Indices.Count;

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
        {
            _vertexArray?.Dispose();
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }

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