using Engine.Renderer.Buffers;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public sealed class SilkNetIndexBuffer : IIndexBuffer
{
    private static readonly ILogger Logger = Log.ForContext<SilkNetIndexBuffer>();
    private uint _rendererId;
    private bool _disposed;

    public SilkNetIndexBuffer(uint[] indices, int count)
    {
        Count = count;

        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _rendererId);
        GLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ElementArrayBuffer)");

        unsafe
        {
            fixed (uint* buf = indices)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)count * sizeof(uint), buf, BufferUsageARB.StaticDraw);
                GLDebug.CheckError(SilkNetContext.GL, "BufferData(IndexBuffer)");
            }
        }
    }
    
    public int Count { get; }

    public void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, _rendererId);
        GLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ElementArrayBuffer)");
    }

    public void Unbind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        GLDebug.CheckError(SilkNetContext.GL, "UnbindBuffer(ElementArrayBuffer)");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
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
            Logger.Error(e, "Failed to delete OpenGL index buffer {RendererId}", _rendererId);
        }

        _disposed = true;
    }
    
    ~SilkNetIndexBuffer()
    {
        Dispose(false);
    }
}