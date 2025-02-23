using Engine.Renderer.Vertex;

namespace Engine.Renderer.Buffers;

public interface IVertexBuffer : IBindable
{
    public void SetLayout(BufferLayout layout);
    public BufferLayout? Layout { get; }
    void SetData(QuadVertex[] vertexes, int dataSize);
    void SetData(LineVertex[] lineVertices, int dataSize);
    void SetData(CircleVertex[] circleVertices, int dataSize);
}