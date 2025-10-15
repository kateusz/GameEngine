using Engine.Renderer;
using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetVertexBuffer : IVertexBuffer
{
    private readonly uint _rendererId;

    public SilkNetVertexBuffer(uint size)
    {
        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);
        unsafe
        {
            SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, size, null, BufferUsageARB.DynamicDraw);
        }
    }

    ~SilkNetVertexBuffer()
    {
        try
        {
            SilkNetContext.GL.DeleteBuffer(_rendererId);
        }
        catch (Exception e)
        {
            // todo: 
        }
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