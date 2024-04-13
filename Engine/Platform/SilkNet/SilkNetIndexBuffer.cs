using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetIndexBuffer : IIndexBuffer
{
    private readonly uint[] _indices;
    private readonly int _count;
    private readonly uint _indexBuffer;

    public SilkNetIndexBuffer(uint[] indices, int count)
    {
        _indices = indices;
        _count = count;


        _indexBuffer = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
    }

    public void Bind()
    {
        unsafe
        {
            fixed (uint* buf = _indices)
                SilkNetContext.GL.BufferData(BufferTargetARB.ElementArrayBuffer,
                    (nuint)(_indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }
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