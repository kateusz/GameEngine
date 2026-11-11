namespace Engine.Renderer.Buffers;

public interface IUniformBuffer : IDisposable
{
    void SetData(ReadOnlySpan<byte> data);
    void BindTo(uint bindingPoint);
}
