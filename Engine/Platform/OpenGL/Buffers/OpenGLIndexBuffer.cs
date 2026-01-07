using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLIndexBuffer : IIndexBuffer
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLIndexBuffer>();
    private uint _rendererId;
    private bool _disposed;

    public OpenGLIndexBuffer(uint[] indices, int count)
    {
        Count = count;

        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ElementArrayBuffer)");

        unsafe
        {
            fixed (uint* buf = indices)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)count * sizeof(uint), buf, BufferUsageARB.StaticDraw);
                OpenGLDebug.CheckError(SilkNetContext.GL, "BufferData(IndexBuffer)");
            }
        }
    }
    
    public int Count { get; }

    public void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ElementArrayBuffer)");
    }

    public void Unbind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindBuffer(ElementArrayBuffer)");
    }

    public void Dispose()
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
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLIndexBuffer()
    {
        if (!_disposed && _rendererId != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: IndexBuffer {_rendererId} not disposed! Count: {Count}"
            );
        }
    }
#endif
}