using Engine.Platform.OpenGL.Buffers;

namespace Engine.Renderer.Buffers;

internal sealed class VertexBufferFactory(IRendererApiConfig apiConfig) : IVertexBufferFactory
{
    public IVertexBuffer Create(uint size)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLVertexBuffer(size),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
