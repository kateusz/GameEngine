using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

internal sealed class HdrToneMapper(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory) : IHdrToneMapper
{
    private IShader _shader;
    private IVertexArray _quadVertexArray;
    private bool _disposed;

    public void RenderToFramebuffer(uint sourceTextureId, IFrameBuffer targetFramebuffer, float exposure = 1.0f)
    {
        targetFramebuffer.Bind();
        rendererApi.SetDepthTest(false);
        rendererApi.SetClearColor(new System.Numerics.Vector4(0f, 0f, 0f, 1f));
        rendererApi.Clear();

        _shader.Bind();
        _shader.SetInt("u_HdrColor", 0);
        _shader.SetFloat("u_Exposure", exposure);
        rendererApi.BindTexture2D(sourceTextureId, 0);

        _quadVertexArray.Bind();
        rendererApi.DrawIndexed(_quadVertexArray, 6);

        rendererApi.SetDepthTest(true);
        targetFramebuffer.Unbind();
    }

    public void Init()
    {
        _shader = shaderFactory.Create("assets/shaders/OpenGL/hdrTonemap.vert",
            "assets/shaders/OpenGL/hdrTonemap.frag");
        _quadVertexArray = CreateFullscreenQuad(vertexArrayFactory, vertexBufferFactory,
            indexBufferFactory);
    }

    private static IVertexArray CreateFullscreenQuad(
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory)
    {
        var vertexArray = vertexArrayFactory.Create();
        var vertexBuffer = vertexBufferFactory.Create((uint)(4 * Mesh.Vertex.GetSize()));

        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float3, "a_Normal"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float3, "a_Tangent"),
            new BufferElement(ShaderDataType.Float3, "a_Bitangent"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);
        vertexBuffer.SetLayout(layout);

        var vertices = new List<Mesh.Vertex>
        {
            new(new System.Numerics.Vector3(-1f, -1f, 0f), System.Numerics.Vector3.UnitZ,
                new System.Numerics.Vector2(0f, 0f), System.Numerics.Vector3.UnitX, System.Numerics.Vector3.UnitY),
            new(new System.Numerics.Vector3(1f, -1f, 0f), System.Numerics.Vector3.UnitZ,
                new System.Numerics.Vector2(1f, 0f), System.Numerics.Vector3.UnitX, System.Numerics.Vector3.UnitY),
            new(new System.Numerics.Vector3(1f, 1f, 0f), System.Numerics.Vector3.UnitZ,
                new System.Numerics.Vector2(1f, 1f), System.Numerics.Vector3.UnitX, System.Numerics.Vector3.UnitY),
            new(new System.Numerics.Vector3(-1f, 1f, 0f), System.Numerics.Vector3.UnitZ,
                new System.Numerics.Vector2(0f, 1f), System.Numerics.Vector3.UnitX, System.Numerics.Vector3.UnitY),
        };
        vertexBuffer.SetMeshData(vertices, vertices.Count * Mesh.Vertex.GetSize());

        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };
        var indexBuffer = indexBufferFactory.Create(indices, indices.Length);

        vertexArray.AddVertexBuffer(vertexBuffer);
        vertexArray.SetIndexBuffer(indexBuffer);
        return vertexArray;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader?.Dispose();
        _quadVertexArray.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}