using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetVertexBuffer : IVertexBuffer
{
    private readonly float[] _vertices;
    private readonly uint _vertexBufferObject;

    public SilkNetVertexBuffer(float[] vertices)
    {
        _vertices = vertices;
        
        _vertexBufferObject = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
    }

    public void SetLayout(BufferLayout layout)
    {
        Layout = layout;
    }

    public BufferLayout Layout { get; private set; }

    public void Bind()
    {
        unsafe
        {
            fixed (float* buf = _vertices)
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float)),
                    buf, BufferUsageARB.StaticDraw);
        }
    }

    public void Unbind()
    {
    }
}