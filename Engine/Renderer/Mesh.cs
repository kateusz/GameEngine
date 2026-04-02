using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

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
    public Texture2D DiffuseTexture { get; set; }
    public List<Texture2D> Textures { get; set; }
    public Matrix4x4 NodeTransform { get; set; } = Matrix4x4.Identity;
    
    private IVertexArray _vertexArray;
    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private bool _initialized = false;
    private bool _disposed = false;
    
    public IVertexArray GetVertexArray()
    {
        if (!_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' not initialized. Call Initialize() before accessing vertex array.");
        return _vertexArray;
    }

    public Mesh(string name = "Unnamed", ITextureFactory? textureFactory = null)
    {
        Name = name;
        Vertices = [];
        Indices = [];
        Textures = [];
        DiffuseTexture = textureFactory?.GetWhiteTexture()!;
    }

    public void Initialize(IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        if (vertexArrayFactory == null)
            throw new ArgumentNullException(nameof(vertexArrayFactory));
        if (vertexBufferFactory == null)
            throw new ArgumentNullException(nameof(vertexBufferFactory));
        if (indexBufferFactory == null)
            throw new ArgumentNullException(nameof(indexBufferFactory));

        if (_initialized)
            throw new InvalidOperationException($"Mesh '{Name}' already initialized. Initialize() should only be called once.");

        // Create vertex array
        _vertexArray = vertexArrayFactory.Create();

        // Create vertex buffer
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

        // Upload vertex data
        UploadVertexData();

        // Create index buffer
        _indexBuffer = indexBufferFactory.Create(Indices.ToArray(), Indices.Count);
        _vertexArray.SetIndexBuffer(_indexBuffer);

        _initialized = true;
    }

    private void UploadVertexData()
    {
        // Upload the mesh vertices directly using our specialized method
        _vertexBuffer.SetMeshData(Vertices, Vertices.Count * Vertex.GetSize());
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

    public int GetIndexCount() => Indices.Count;

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
                $"Vertices: {Vertices.Count}, Indices: {Indices.Count}"
            );
        }
        Dispose(false);
    }
#endif
}