using Engine.Renderer.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenGL;

public class OpenGLIndexBuffer : IIndexBuffer
{
    private readonly uint[] _indices;
    private readonly int _count;
    private readonly int _indexBuffer;

    public OpenGLIndexBuffer(uint[] indices, int count)
    {
        _indices = indices;
        _count = count;
        
        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
    }

    public void Bind()
    {
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);
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