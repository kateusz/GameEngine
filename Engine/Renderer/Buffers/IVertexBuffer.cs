namespace Engine.Renderer.Buffers;

public interface IVertexBuffer : IBindable
{
    public void SetLayout(BufferLayout layout);
    public BufferLayout Layout { get; }
}