using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetIndexBuffer : IIndexBuffer
{
    private uint _rendererId;
    private bool _disposed;

    public SilkNetIndexBuffer(uint[] indices, int count)
    {
        Count = count;

        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _rendererId);

        unsafe
        {
            fixed (uint* buf = indices)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)count * sizeof(uint), buf, BufferUsageARB.StaticDraw);
            }
        }
    }

    ~SilkNetIndexBuffer()
    {
        Dispose(false);
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        try
        {
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteBuffer(_rendererId);
                _rendererId = 0;
            }
        }
        catch (Exception e)
        {
            // Finalizers and Dispose must not throw exceptions
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL index buffer {_rendererId}: {e.Message}");
        }

        _disposed = true;
    }
}