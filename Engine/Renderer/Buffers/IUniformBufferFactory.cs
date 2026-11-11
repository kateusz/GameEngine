namespace Engine.Renderer.Buffers;

public interface IUniformBufferFactory
{
    IUniformBuffer Create(int byteSize);
}
