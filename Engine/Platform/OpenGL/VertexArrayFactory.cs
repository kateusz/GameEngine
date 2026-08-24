using Engine.Renderer.Buffers.VertexArray;

namespace Engine.Platform.OpenGL;

internal sealed class VertexArrayFactory : IVertexArrayFactory
{
    public IVertexArray Create() => new OpenGLVertexArray();
}
