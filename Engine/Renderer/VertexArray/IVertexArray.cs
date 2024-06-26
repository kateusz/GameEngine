using Engine.Renderer.Buffers;

namespace Engine.Renderer.VertexArray;

public interface IVertexArray : IBindable
{
    public IList<IVertexBuffer> VertexBuffers { get; }
    public IIndexBuffer IndexBuffer { get; }
    
    void AddVertexBuffer(IVertexBuffer vertexBuffer);
    void SetIndexBuffer(IIndexBuffer indexBuffer);
}