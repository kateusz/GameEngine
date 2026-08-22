using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Meshes;

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

    public IVertexBuffer Create(List<Mesh.Vertex> vertices)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLVertexBuffer(vertices),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
