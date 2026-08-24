using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Buffers;

namespace Engine.Platform.OpenGL;

internal sealed class IndexBufferFactory : IIndexBufferFactory
{
    public IIndexBuffer Create(uint[] indices, int count) => new OpenGLIndexBuffer(indices, count);
}
