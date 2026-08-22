using System.Runtime.InteropServices;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline.Primitives;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLVertexBuffer : IVertexBuffer
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLVertexBuffer>();
    private bool _disposed;

    internal uint RendererId { get; private set; }

    // Maximum buffer size limit: 256 MB
    // This prevents accidental allocation of excessive GPU memory which could lead to:
    // - Out-of-memory crashes
    // - System instability
    // - Difficult debugging of size calculation errors
    private const uint MaxBufferSize = 256 * 1024 * 1024;

    /// <summary>
    /// Allocates an empty GPU buffer of <paramref name="size"/> bytes with
    /// <see cref="BufferUsageARB.DynamicDraw"/>.
    /// </summary>
    /// <param name="size">Capacity in bytes. Must be greater than zero and at most 256 MB.</param>
    /// <remarks>
    /// This is the 2D batch path (<c>Graphics2D</c> quads and lines). The buffer is sized once
    /// and rewritten every frame with <c>BufferSubData</c>, so the driver hint must be
    /// <c>DynamicDraw</c>. An empty <c>BufferData(..., null)</c> is intentional: there is no
    /// vertex payload at construction time.
    /// <para>
    /// Do not use this constructor for static meshes. That used to allocate <c>DynamicDraw</c>
    /// here and then replace the store with a second <c>BufferData(StaticDraw)</c> — a wasted
    /// GPU allocation. Meshes go through <see cref="OpenGLVertexBuffer(List{Mesh.Vertex})"/>,
    /// which uploads vertices in one <c>BufferData(StaticDraw)</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="size"/> is 0 or exceeds the 256 MB cap.
    /// </exception>
    public OpenGLVertexBuffer(uint size)
    {
        unsafe
        {
            Allocate(size, BufferUsageARB.DynamicDraw, null);
        }
    }

    public OpenGLVertexBuffer(List<Mesh.Vertex> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertices.Count == 0)
            throw new ArgumentException("Vertex list must not be empty", nameof(vertices));

        var byteSpan = MemoryMarshal.Cast<Mesh.Vertex, byte>(CollectionsMarshal.AsSpan(vertices));
        unsafe
        {
            fixed (byte* pData = byteSpan)
                Allocate((uint)byteSpan.Length, BufferUsageARB.StaticDraw, pData);
        }
    }

    private unsafe void Allocate(uint size, BufferUsageARB usage, void* data)
    {
        switch (size)
        {
            case 0:
                throw new ArgumentException("Buffer size must be greater than zero", nameof(size));
            case > MaxBufferSize:
                throw new ArgumentException($"Buffer size {size} bytes exceeds maximum {MaxBufferSize} bytes ({MaxBufferSize / (1024 * 1024)} MB)", nameof(size));
        }

        RendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, RendererId);

        try
        {
            SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, size, data, usage);
            OpenGLDebug.CheckError(SilkNetContext.GL, $"ArrayBuffer BufferData {usage}");
        }
        catch
        {
            SilkNetContext.GL.DeleteBuffer(RendererId);
            throw;
        }
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
            Logger.Error(e, "Failed to delete OpenGL vertex buffer {RendererId}", RendererId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLVertexBuffer()
    {
        if (!_disposed && RendererId != 0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"GPU LEAK: VertexBuffer {RendererId} not disposed!"
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

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, RendererId);
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

        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, RendererId);
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

    public void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, RendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindBuffer(ArrayBuffer)");
    }

    public void Unbind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindBuffer(ArrayBuffer)");
    }
}