using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public record CameraData
{
    public Matrix4x4 ViewProjection { get; set; }

    public static int GetSize() => 64;
}

public class Renderer2DData
{
    private const int MaxQuads = 10;

    public const int MaxVertices = MaxQuads * 4; // 4 vertex per quad
    public const int MaxIndices = MaxQuads * 6; // 6 indices oer quad
    public const int MaxTextureSlots = 16;
    public const float LineWidth = 1.0f;

    public IVertexArray QuadVertexArray { get; set; }
    public IVertexBuffer QuadVertexBuffer { get; set; }
    public IShader QuadShader { get; set; }
    public Texture2D WhiteTexture { get; set; }
    public List<QuadVertex> QuadVertexBufferBase { get; set; } = [];
    public int CurrentVertexBufferIndex { get; set; }
    public uint QuadIndexBufferCount { get; set; }
    public readonly List<Vector4> QuadVertexPositions = new(4);

    public IVertexArray LineVertexArray { get; set; }
    public IVertexBuffer LineVertexBuffer { get; set; }
    public IShader LineShader { get; set; }
    public List<LineVertex> LineVertexBufferBase { get; set; } = [];
    public int CurrentLineVertexBufferIndex { get; set; }
    public uint LineVertexCount { get; set; }


    public readonly Texture2D[] TextureSlots = new Texture2D[MaxTextureSlots];
    public int TextureSlotIndex { get; set; }
    public Statistics Stats { get; set; }
    

    public CameraData CameraBuffer { get; set; }
    public IUniformBuffer CameraUniformBuffer { get; set; }
}