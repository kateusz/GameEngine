using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetIndexBuffer : IIndexBuffer
{
    private readonly uint _rendererId;

    public SilkNetIndexBuffer(uint[] indices, int count)
    {
        Count = count;
        
        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);
        
        unsafe
        {
            fixed (uint* buf = indices)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)count * sizeof(uint), buf, BufferUsageARB.StaticDraw);
            }
        }
    }

    ~SilkNetIndexBuffer()
    {
        SilkNetContext.GL.DeleteBuffers(1, _rendererId);
    }
    
    public int Count { get; }

    public void Bind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, _rendererId);
 
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, 0);
    }
}