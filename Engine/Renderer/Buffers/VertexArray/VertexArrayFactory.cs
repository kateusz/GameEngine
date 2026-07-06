using Engine.Platform.OpenGL;

namespace Engine.Renderer.Buffers.VertexArray;

internal sealed class VertexArrayFactory(IRendererApiConfig apiConfig) : IVertexArrayFactory
{
    public IVertexArray Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLVertexArray(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
