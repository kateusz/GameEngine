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

    public string Name { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<uint> Indices { get; set; }

    internal List<uint[]> LodIndices { get; } = [];
    internal List<float> LodErrors { get; } = [];

    private IIndexBuffer[] _lodBuffers = [];
    private int _level0IndexCount;

    /// <summary>Vertex count captured at <see cref="Initialize"/>, kept after CPU buffers are released.</summary>
    public int VertexCount { get; private set; }

    /// <summary>Local positions box captured at <see cref="Initialize"/>, kept after CPU vertices are released.</summary>
    internal Aabb? Bounds { get; private set; }

    /// <summary>CPU vertex snapshot retained by source meshes for world-space concatenation.</summary>
    internal Vertex[]? BakeVertices { get; private set; }

    /// <summary>CPU index snapshot retained by source meshes for world-space concatenation (LOD 0).</summary>
    internal uint[]? BakeIndices { get; private set; }

    /// <summary>CPU index snapshots for LOD levels 1..N (same order as <see cref="LodIndices"/> at Initialize).</summary>
    internal uint[][]? BakeLodIndices { get; private set; }
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
        IIndexBufferFactory indexBufferFactory) =>
        Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory, retainBakeGeometry: true);

    internal void Initialize(IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory, bool retainBakeGeometry)
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

        uint[] uploadedIndices = [.. Indices];
        var indexBuffer = indexBufferFactory.Create(uploadedIndices, uploadedIndices.Length);
        var buffers = new List<IIndexBuffer>(1 + LodIndices.Count) { indexBuffer };
        foreach (var lod in LodIndices)
        {
            try
            {
                var copy = lod.ToArray();
                buffers.Add(indexBufferFactory.Create(copy, copy.Length));
            }
            catch (Exception)
            {
                break;
            }
        }

        var extra = buffers.Count - 1;
        if (LodErrors.Count > extra)
            LodErrors.RemoveRange(extra, LodErrors.Count - extra);

        _lodBuffers = buffers.ToArray();
        _level0IndexCount = uploadedIndices.Length;
        _vertexArray.SetIndexBuffer(_lodBuffers[0]);

        _initialized = true;
        VertexCount = Vertices.Count;
        Bounds = Aabb.FromPositions(Vertices);

        if (retainBakeGeometry)
        {
            BakeVertices = [.. Vertices];
            BakeIndices = uploadedIndices;
            if (LodIndices.Count == 0)
            {
                BakeLodIndices = null;
            }
            else
            {
                BakeLodIndices = new uint[LodIndices.Count][];
                for (var i = 0; i < LodIndices.Count; i++)
                    BakeLodIndices[i] = LodIndices[i].ToArray();
            }
        }

        Vertices.Clear();
        Vertices.TrimExcess();
        Indices.Clear();
        Indices.TrimExcess();
        LodIndices.Clear();
        LodIndices.TrimExcess();
    }

    /// <summary>CPU indices for the drawn LOD level, or null when that level was not retained.</summary>
    internal uint[]? BakeIndicesAt(int level)
    {
        if (level <= 0)
            return BakeIndices;
        if (BakeLodIndices is null || level > BakeLodIndices.Length)
            return null;
        return BakeLodIndices[level - 1];
    }

    internal int LodCount => _initialized ? _lodBuffers.Length : 1 + LodIndices.Count;

    internal void BindLevel(int level)
    {
        Bind();
        var index = System.Math.Clamp(level, 0, _lodBuffers.Length - 1);
        _vertexArray.SetIndexBuffer(_lodBuffers[index]);
    }

    internal int IndexCountAt(int level) =>
        _initialized
            ? _lodBuffers[System.Math.Clamp(level, 0, _lodBuffers.Length - 1)].Count
            : Indices.Count;

    internal float LodError(int level) =>
        level <= 0 || level > LodErrors.Count ? 0f : LodErrors[level - 1];

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

    public int GetIndexCount() => _initialized ? _level0IndexCount : Indices.Count;

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
            foreach (var buffer in _lodBuffers)
                buffer.Dispose();
            _lodBuffers = [];
            _vertexArray?.Dispose();
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
