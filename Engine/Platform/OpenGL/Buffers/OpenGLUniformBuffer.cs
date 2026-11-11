using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLUniformBuffer : IUniformBuffer
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLUniformBuffer>();
    private const uint MaxBufferSize = 256 * 1024 * 1024;
    private readonly int _byteSize;
    private uint _id;
    private bool _disposed;

    public OpenGLUniformBuffer(int byteSize)
    {
        if (byteSize <= 0)
            throw new ArgumentException("Buffer size must be greater than zero", nameof(byteSize));
        if (byteSize > MaxBufferSize)
            throw new ArgumentException(
                $"Buffer size {byteSize} bytes exceeds maximum {MaxBufferSize} bytes", nameof(byteSize));

        _byteSize = byteSize;
        _id = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _id);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(UniformBuffer)");
        unsafe
        {
            SilkNetContext.GL.BufferData(BufferTargetARB.UniformBuffer, (nuint)byteSize, null, BufferUsageARB.DynamicDraw);
        }
        OpenGLDebug.CheckError(SilkNetContext.GL, "BufferData(UniformBuffer)");
    }

    public void SetData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (data.Length != _byteSize)
            throw new ArgumentException($"Expected {_byteSize} bytes, got {data.Length}", nameof(data));

        SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _id);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(UniformBuffer)");
        unsafe
        {
            fixed (byte* p = data)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)data.Length, p);
            }
        }
        OpenGLDebug.CheckError(SilkNetContext.GL, "BufferSubData(UniformBuffer)");
    }

    public void BindTo(uint bindingPoint)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, _id);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBufferBase(UniformBuffer)");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_id != 0)
            {
                SilkNetContext.GL.DeleteBuffer(_id);
                _id = 0;
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to delete OpenGL uniform buffer {Id}", _id);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
