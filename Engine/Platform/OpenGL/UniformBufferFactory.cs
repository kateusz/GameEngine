using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Buffers;

namespace Engine.Platform.OpenGL;

internal sealed class UniformBufferFactory : IUniformBufferFactory
{
    public IUniformBuffer Create(int byteSize) => new OpenGLUniformBuffer(byteSize);
}
