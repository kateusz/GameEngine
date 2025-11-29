using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

internal sealed class Renderer2DData
{
    public const int MaxVertices = RenderingConstants.MaxVertices;
    public const int MaxIndices = RenderingConstants.MaxIndices;
    public const int MaxTextureSlots = RenderingConstants.MaxTextureSlots;
    public const float LineWidth = RenderingConstants.DefaultLineWidth;

    public IVertexArray QuadVertexArray { get; internal set; }
    public IVertexBuffer QuadVertexBuffer { get; internal set; }
    public IShader QuadShader { get; internal set; }
    public Texture2D WhiteTexture { get; internal set; }
    public QuadVertex[] QuadVertexBufferBase = new QuadVertex[MaxVertices];
    public int CurrentVertexBufferIndex { get; internal set; }
    public uint QuadIndexBufferCount { get; internal set; }
    public readonly Vector4[] QuadVertexPositions = new Vector4[RenderingConstants.QuadVertexCount];

    public IVertexArray LineVertexArray { get; internal set; }
    public IVertexBuffer LineVertexBuffer { get; internal set; }
    public IShader LineShader { get; internal set; }
    public LineVertex[] LineVertexBufferBase = new LineVertex[MaxVertices];
    public int CurrentLineVertexBufferIndex { get; internal set; }
    public uint LineVertexCount { get; internal set; }


    public readonly Texture2D[] TextureSlots = new Texture2D[MaxTextureSlots];
    public int TextureSlotIndex { get; internal set; }
    public readonly Dictionary<uint, int> TextureSlotCache = new();
    public Statistics Stats { get; internal set; }
}