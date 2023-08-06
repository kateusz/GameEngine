using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenGL;

public class OpenGLIndexBuffer : IIndexBuffer
{
    private readonly int _count;
    private readonly int _indexBuffer;

    public OpenGLIndexBuffer(int[] indices, int count)
    {
        _count = count;
        _indexBuffer = GL.GenBuffer();
        GL.BufferData(BufferTarget.ElementArrayBuffer, count * sizeof(int), indices, BufferUsageHint.StaticDraw);
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
    }

    public void Unbind()
    {
        throw new NotImplementedException();
    }

    public int GetCount()
    {
        return _count;
    }
}