using System.Runtime.InteropServices;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLVertexBuffer : IVertexBuffer
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLVertexBuffer>();
    private uint _rendererId;
    private bool _disposed;

    // Maximum buffer size limit: 256 MB
    // This prevents accidental allocation of excessive GPU memory which could lead to:
    // - Out-of-memory crashes
    // - System instability
    // - Difficult debugging of size calculation errors
    private const uint MaxBufferSize = 256 * 1024 * 1024;

    public OpenGLVertexBuffer(uint size)
    {
        switch (size)
        {
            // Validate buffer size to prevent memory allocation issues
            case 0:
                throw new ArgumentException("Buffer size must be greater than zero", nameof(size));
            case > MaxBufferSize:
                throw new ArgumentException($"Buffer size {size} bytes exceeds maximum {MaxBufferSize} bytes ({MaxBufferSize / (1024 * 1024)} MB)", nameof(size));
        }

        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);

        try
        {
            unsafe
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, size, null, BufferUsageARB.DynamicDraw);
                OpenGLDebug.CheckError(SilkNetContext.GL, "ArrayBuffer BufferData DynamicDraw");
            }
        }
        catch
        {
            SilkNetContext.GL.DeleteBuffer(_rendererId);
            throw;
        }
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
            Logger.Error(e, "Failed to delete OpenGL vertex buffer {RendererId}", _rendererId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLVertexBuffer()
    {
        if (!_disposed && _rendererId != 0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"GPU LEAK: VertexBuffer {_rendererId} not disposed!"
            );
        }
    }
#endif

    public void SetLayout(BufferLayout layout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Layout = layout;
    }

    public BufferLayout? Layout { get; private set; }

    public void SetData(Span<QuadVertex> vertices, int dataSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ArrayBuffer)");

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = MemoryMarshal.Cast<QuadVertex, byte>(vertices);
            fixed (byte* pData = vertexSpan)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)dataSize, pData);
                OpenGLDebug.CheckError(SilkNetContext.GL, "BufferSubData(QuadVertex)");
            }
        }
    }

    public void SetData(Span<LineVertex> vertices, int dataSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ArrayBuffer)");

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = MemoryMarshal.Cast<LineVertex, byte>(vertices);
            fixed (byte* pData = vertexSpan)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)dataSize, pData);
                OpenGLDebug.CheckError(SilkNetContext.GL, "BufferSubData(LineVertex)");
            }
        }
    }

    // In OpenGLVertexBuffer.cs, modify SetMeshData to handle large data in chunks
    public void SetMeshData(List<Mesh.Vertex> vertices, int dataSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Count == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ArrayBuffer)");

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = CollectionsMarshal.AsSpan(vertices);
            var byteSpan = MemoryMarshal.Cast<Mesh.Vertex, byte>(vertexSpan);
            fixed (byte* pData = byteSpan)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)byteSpan.Length, pData,
                    BufferUsageARB.StaticDraw);
                OpenGLDebug.CheckError(SilkNetContext.GL, "BufferData(MeshVertex)");
            }
        }
    }

    public void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ArrayBuffer)");
    }

    public void Unbind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindBuffer(ArrayBuffer)");
    }
}