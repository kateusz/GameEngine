namespace Engine.Renderer.Buffers;

public interface IUniformBuffer : IDisposable
{
    void Update(ReadOnlySpan<float> std140);
    void BindBase(int bindingPoint);
}
