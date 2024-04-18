using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetIndexBuffer : IIndexBuffer
{
    private readonly uint[] _indices;
    private readonly uint _indexBuffer;

    public SilkNetIndexBuffer(uint[] indices, int count)
    {
        _indices = indices;
        Count = count;
        
        _indexBuffer = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
    }
    
    public int Count { get; }

    public void Bind()
    {
        unsafe
        {
            fixed (uint* buf = _indices)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ElementArrayBuffer,
                    (nuint)(_indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
            }
        }
    }

    public void Unbind()
    {
        SilkNetContext.GL.DeleteBuffer(_indexBuffer);
    }
}