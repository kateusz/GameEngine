using Engine.Renderer;
using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetVertexBuffer : IVertexBuffer
{
    private uint _rendererId;
    private bool _disposed;

    // Maximum buffer size limit: 256 MB
    // This prevents accidental allocation of excessive GPU memory which could lead to:
    // - Out-of-memory crashes
    // - System instability
    // - Difficult debugging of size calculation errors
    private const uint MaxBufferSize = 256 * 1024 * 1024;

    public SilkNetVertexBuffer(uint size)
    {
        // Validate buffer size to prevent memory allocation issues
        if (size == 0)
            throw new ArgumentException("Buffer size must be greater than zero", nameof(size));

        if (size > MaxBufferSize)
            throw new ArgumentException($"Buffer size {size} bytes exceeds maximum {MaxBufferSize} bytes ({MaxBufferSize / (1024 * 1024)} MB)", nameof(size));

        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);

        try
        {
            unsafe
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, size, null, BufferUsageARB.DynamicDraw);
            }

            // Check for OpenGL errors after buffer allocation
            var error = SilkNetContext.GL.GetError();
            if (error != GLEnum.NoError)
            {
                throw new InvalidOperationException($"OpenGL error during vertex buffer creation: {error}");
            }
        }
        catch
        {
            // Clean up buffer on failure to prevent resource leak
            SilkNetContext.GL.DeleteBuffer(_rendererId);
            throw;
        }
    }

    ~SilkNetVertexBuffer()
    {
        Dispose(false);
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
            // Log the error but suppress it to prevent application crashes
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL vertex buffer {_rendererId}: {e.Message}");
        }

        _disposed = true;
    }

    public void SetLayout(BufferLayout layout)
    {
        Layout = layout;
    }

    public BufferLayout? Layout { get; private set; }

    public void SetData(Span<QuadVertex> vertices, int dataSize)
    {
        if (vertices.Length == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = MemoryMarshal.Cast<QuadVertex, byte>(vertices);
            fixed (byte* pData = vertexSpan)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)dataSize, pData);
            }
        }
    }

    public void SetData(Span<LineVertex> lineVertices, int dataSize)
    {
        if (lineVertices.Length == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = MemoryMarshal.Cast<LineVertex, byte>(lineVertices);
            fixed (byte* pData = vertexSpan)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)dataSize, pData);
            }
        }
    }

    // In SilkNetVertexBuffer.cs, modify SetMeshData to handle large data in chunks
    public void SetMeshData(List<Mesh.Vertex> vertices, int dataSize)
    {
        if (vertices.Count == 0)
            return;

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        unsafe
        {
            // Use Span<T> for direct memory access without allocations
            var vertexSpan = CollectionsMarshal.AsSpan(vertices);
            var byteSpan = MemoryMarshal.Cast<Mesh.Vertex, byte>(vertexSpan);
            fixed (byte* pData = byteSpan)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)byteSpan.Length, pData,
                    BufferUsageARB.StaticDraw);
            }
        }
    }

    public void Bind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, 0);
    }
}