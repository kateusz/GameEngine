using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer;
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

        // GLSL layout(location): 0 Position, 1 Normal, 2 TexCoord, 3 Tangent, 4 Bitangent, 5 EntityID.
        // cube.vert skips 3–4 so EntityID stays at 5.
        public static BufferLayout Layout { get; } = new([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float3, "a_Tangent"),
            new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct SkinnedVertex(
        Vector3 Position,
        Vector3 Normal,
        Vector2 TexCoord,
        Vector3 Tangent,
        Vector3 Bitangent,
        int EntityId,
        int Joint0,
        int Joint1,
        int Joint2,
        int Joint3,
        Vector4 Weights)
    {
        public static BufferLayout Layout { get; } = new([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float3, "a_Tangent"),
            new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
            new BufferElement(ShaderDataType.Int, "a_EntityID"),
            new BufferElement(ShaderDataType.Int4, "a_Joints"),
            new BufferElement(ShaderDataType.Float4, "a_Weights")
        ]);
    }

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }
    public bool IsSkinned { get; set; }
    public int[] Joints { get; set; } = [];
    public float[] Weights { get; set; } = [];

    /// <summary>Vertex count captured at <see cref="Initialize"/>, kept after CPU buffers are released.</summary>
    public int VertexCount { get; private set; }

    /// <summary>Local positions box captured at <see cref="Initialize"/>, kept after CPU vertices are released.</summary>
    internal Aabb? Bounds { get; private set; }
    public Texture2D? DiffuseTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }
    public Texture2D? MetallicRoughnessTexture { get; set; }
    public Texture2D? OcclusionTexture { get; set; }
    public float MetallicFactor { get; set; }
    public float RoughnessFactor { get; set; } = 0.5f;
    public Vector3 BaseColorFactor { get; set; } = Vector3.One;
    public Vector3 EmissiveFactor { get; set; }
    public Texture2D? EmissiveTexture { get; set; }
    public bool HasEmissiveMap => EmissiveTexture != null;
    public bool AlphaCutout { get; set; }
    public bool DoubleSided { get; set; }
    public float AlphaCutoff { get; set; } = 0.5f;

    public bool HasDiffuseMap => DiffuseTexture != null;
    public bool HasNormalMap => NormalTexture != null;
    public bool HasMetallicRoughnessMap => MetallicRoughnessTexture != null;
    public bool HasOcclusionMap => OcclusionTexture != null;

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
        var vertexBuffer = IsSkinned
            ? vertexBufferFactory.Create(PackSkinnedVertices())
            : vertexBufferFactory.Create(Vertices);
        vertexBuffer.SetLayout(IsSkinned ? SkinnedVertex.Layout : Vertex.Layout);
        _vertexArray.AddVertexBuffer(vertexBuffer);

        var indexBuffer = indexBufferFactory.Create([.. Indices], Indices.Count);
        _vertexArray.SetIndexBuffer(indexBuffer);

        _initialized = true;
        VertexCount = Vertices.Count;
        Bounds = Aabb.FromPositions(Vertices);

        Vertices.Clear();
        Vertices.TrimExcess();
        Indices.Clear();
        Indices.TrimExcess();
        Joints = [];
        Weights = [];
    }

    private List<SkinnedVertex> PackSkinnedVertices()
    {
        var packed = new List<SkinnedVertex>(Vertices.Count);
        for (var i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            var start = i * 4;
            packed.Add(new SkinnedVertex(
                vertex.Position,
                vertex.Normal,
                vertex.TexCoord,
                vertex.Tangent,
                vertex.Bitangent,
                -1,
                JointAt(start),
                JointAt(start + 1),
                JointAt(start + 2),
                JointAt(start + 3),
                new Vector4(WeightAt(start), WeightAt(start + 1), WeightAt(start + 2), WeightAt(start + 3))));
        }

        return packed;
    }

    private int JointAt(int index) => (uint)index < (uint)Joints.Length ? Joints[index] : 0;

    private float WeightAt(int index) => (uint)index < (uint)Weights.Length ? Weights[index] : 0f;

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