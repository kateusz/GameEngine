using Engine.Renderer;
using Engine.Renderer.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenTK;

public class OpenTKVertexBuffer : IVertexBuffer
{
    private readonly float[] _vertices;
    private readonly int _vertexBufferObject;

    public OpenTKVertexBuffer(float[] vertices)
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
    public void SetData(QuadVertex[] toArray, uint dataSize)
    {
        throw new NotImplementedException();
    }

    public void Bind()
    {
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
    }

    public void Unbind()
    {
    }
}