namespace Engine.Renderer;

public interface IVertexBuffer : IBindable
{
    public void SetLayout(BufferLayout layout);
    public BufferLayout Layout { get; }
}