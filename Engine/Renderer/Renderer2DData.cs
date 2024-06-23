using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public class Renderer2DData
{
    public const int MaxQuads = 10;
    public const int MaxVertices = MaxQuads * 4; // 4 vertex per quad
    public const int MaxIndices = MaxQuads * 6; // 6 indices oer quad

    public IVertexArray QuadVertexArray { get; set; }
    public IVertexBuffer QuadVertexBuffer { get; set; }
    public IShader TextureShader { get; set; }
    public Texture2D WhiteTexture { get; set; }

    public List<QuadVertex> QuadVertexBufferBase { get; set; } = [];
    public int CurrentVertexBufferIndex { get; set; }
    public uint QuadIndexBufferCount { get; set; }
    public List<Vector4> QuadVertexPositions = new(4);
}