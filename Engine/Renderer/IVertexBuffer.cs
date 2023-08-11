namespace Engine.Renderer;

public interface IVertexBuffer : IBindable
{
    public BufferLayout Layout { get; set; }
}