using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenGL;

public class OpenGLVertexArray : IVertexArray
{
    private int _vertexArrayObject;

    public OpenGLVertexArray()
    {
        _vertexArrayObject = GL.GenVertexArray();
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; private set; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        GL.BindVertexArray(_vertexArrayObject);
    }

    public void Unbind()
    {
        // not needed?
        GL.BindVertexArray(0);
    }

    public void AddVertexBuffer(IVertexBuffer vertexBuffer)
    {
        GL.BindVertexArray(_vertexArrayObject);
        
        vertexBuffer.Bind();
        
        if (vertexBuffer.Layout is null)
        {
            throw new InvalidOperationException("Vertex buffer has no layout!");
        }
        
        var layout = vertexBuffer.Layout;
        
        for (var index = 0; index < layout.Elements.Count; index++)
        {
            var element = layout.Elements[index];
            GL.EnableVertexAttribArray(index);
            GL.VertexAttribPointer(index, 
                element.GetComponentCount(),
                element.Type.ToGLBaseType(),
                element.Normalized,
                layout.Stride,
                element.Offset);
        }
        
        VertexBuffers.Add(vertexBuffer);
    }

    public void SetIndexBuffer(IIndexBuffer indexBuffer)
    {
        GL.BindVertexArray(_vertexArrayObject);
        indexBuffer.Bind();

        IndexBuffer = indexBuffer;
    }
}