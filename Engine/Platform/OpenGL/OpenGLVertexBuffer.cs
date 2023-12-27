using Engine.Renderer.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenGL;

public class OpenGLVertexBuffer : IVertexBuffer
{
    private readonly float[] _vertices;
    private readonly int _vertexBufferObject;

    public OpenGLVertexBuffer(float[] vertices)
    {
        _vertices = vertices;
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
    }

    public void SetLayout(BufferLayout layout)
    {
        Layout = layout;
    }

    public BufferLayout Layout { get; private set; }
    
    public void Bind()
    {
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
    }

    public void Unbind()
    {
    }

   
}