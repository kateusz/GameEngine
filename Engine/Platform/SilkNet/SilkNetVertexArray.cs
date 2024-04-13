using Engine.Renderer.Buffers;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetVertexArray : IVertexArray
{
    private readonly uint _vertexArrayObject;

    public SilkNetVertexArray()
    {
        _vertexArrayObject = SilkNetContext.GL.GenVertexArray();
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; private set; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
    }

    public void Unbind()
    {
        // not needed?
        SilkNetContext.GL.BindVertexArray(0);
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    public void AddVertexBuffer(IVertexBuffer vertexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
        
        vertexBuffer.Bind();
        
        if (vertexBuffer.Layout is null)
        {
            throw new InvalidOperationException("Vertex buffer has no layout!");
        }
        
        var layout = vertexBuffer.Layout;
        
        for (int index = 0; index < layout.Elements.Count; index++)
        {
            unsafe
            {
                var element = layout.Elements[index];
                SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                SilkNetContext.GL.VertexAttribPointer((uint)index, 
                    element.GetComponentCount(),
                    VertexAttribPointerType.Float,
                    element.Normalized,
                    (uint)layout.Stride,
                    (void*)0);
            }
        }
        
        VertexBuffers.Add(vertexBuffer);
    }

    public void SetIndexBuffer(IIndexBuffer indexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
        indexBuffer.Bind();

        IndexBuffer = indexBuffer;
    }
}