namespace Engine.Renderer.Buffers;

public interface IVertexBuffer : IBindable
{
    public void SetLayout(BufferLayout layout);
    public BufferLayout? Layout { get; }
    void SetData(Span<QuadVertex> vertices, int dataSize);
    void SetData(Span<LineVertex> vertices, int dataSize);
    void SetMeshData(List<Mesh.Vertex> vertices, int dataSize);
}