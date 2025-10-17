using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public class Renderer2DData
{
    public const int MaxVertices = RenderingConstants.MaxVertices;
    public const int MaxIndices = RenderingConstants.MaxIndices;
    public const int MaxTextureSlots = RenderingConstants.MaxTextureSlots;
    public const float LineWidth = RenderingConstants.DefaultLineWidth;

    public IVertexArray QuadVertexArray { get; set; }
    public IVertexBuffer QuadVertexBuffer { get; set; }
    public IShader QuadShader { get; set; }
    public Texture2D WhiteTexture { get; set; }
    public QuadVertex[] QuadVertexBufferBase = new QuadVertex[MaxVertices];
    public int CurrentVertexBufferIndex { get; set; }
    public uint QuadIndexBufferCount { get; set; }
    public readonly Vector4[] QuadVertexPositions = new Vector4[RenderingConstants.QuadVertexCount];

    public IVertexArray LineVertexArray { get; set; }
    public IVertexBuffer LineVertexBuffer { get; set; }
    public IShader LineShader { get; set; }
    public LineVertex[] LineVertexBufferBase = new LineVertex[MaxVertices];
    public int CurrentLineVertexBufferIndex { get; set; }
    public uint LineVertexCount { get; set; }


    public readonly Texture2D[] TextureSlots = new Texture2D[MaxTextureSlots];
    public int TextureSlotIndex { get; set; }
    public Statistics Stats { get; set; }
}