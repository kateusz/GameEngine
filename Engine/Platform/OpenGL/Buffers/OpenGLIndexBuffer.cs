using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLIndexBuffer : IIndexBuffer
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLIndexBuffer>();
    private bool _disposed;

    internal uint RendererId { get; private set; }

    public OpenGLIndexBuffer(uint[] indices, int count)
    {
        Count = count;

        RendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, RendererId);
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
        SilkNetContext.GL.BindBuffer(GLEnum.ElementArrayBuffer, RendererId);
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
            if (RendererId != 0)
            {
                SilkNetContext.GL.DeleteBuffer(RendererId);
                RendererId = 0;
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to delete OpenGL index buffer {RendererId}", RendererId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLIndexBuffer()
    {
        if (!_disposed && RendererId != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: IndexBuffer {RendererId} not disposed! Count: {Count}"
            );
        }
    }
#endif
}