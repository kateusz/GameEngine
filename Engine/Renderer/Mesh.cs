using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public class Mesh
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public int EntityId;

        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, int entityId = -1)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
            EntityId = entityId;
        }

        public static int GetSize() => sizeof(float) * (3 + 3 + 2) + sizeof(int);
    }

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }
    public Texture2D DiffuseTexture { get; set; }
    
    private IVertexArray _vertexArray;
    private IVertexBuffer _vertexBuffer;
    private IIndexBuffer _indexBuffer;
    private bool _initialized = false;
    
    public IVertexArray GetVertexArray()
    {
        if (!_initialized)
            Initialize();
        return _vertexArray;
    }

    public Mesh(string name = "Unnamed")
    {
        Name = name;
        Vertices = new List<Vertex>();
        Indices = new List<uint>();
        DiffuseTexture = TextureFactory.Create(1, 1); // Default white texture
    }

    public void Initialize()
    {
        if (_initialized) return;
        
        // Create vertex array
        _vertexArray = VertexArrayFactory.Create();
        
        // Create vertex buffer
        _vertexBuffer = VertexBufferFactory.Create((uint)(Vertices.Count * Vertex.GetSize()));
        
        var layout = new BufferLayout(new []
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        });
        
        _vertexBuffer.SetLayout(layout);
        _vertexArray.AddVertexBuffer(_vertexBuffer);
        
        // Upload vertex data
        UploadVertexData();
        
        // Create index buffer
        _indexBuffer = IndexBufferFactory.Create(Indices.ToArray(), Indices.Count);
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
            Initialize();
            
        _vertexArray.Bind();
        DiffuseTexture.Bind();
    }

    public void Unbind()
    {
        _vertexArray.Unbind();
    }

    public int GetIndexCount() => Indices.Count;
}