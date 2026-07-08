using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.Buffers.VertexArray;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLVertexArray : IVertexArray
{
    private bool _disposed;

    internal uint RendererId { get; private set; }

    public OpenGLVertexArray()
    {
        RendererId = SilkNetContext.GL.GenVertexArray();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenVertexArray");
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        SilkNetContext.GL.BindVertexArray(RendererId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindVertexArray");
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindVertexArray(0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindVertexArray");
    }

    public void AddVertexBuffer(IVertexBuffer vertexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(RendererId);

        vertexBuffer.Bind();

        if (vertexBuffer.Layout is null)
            throw new InvalidOperationException("Vertex buffer has no layout!");

        var layout = vertexBuffer.Layout.Value; // Access the struct value

        for (var index = 0; index < layout.Elements.Count; index++)
        {
            unsafe
            {
                var element = layout.Elements[index];

                switch (element.Type.ToBaseType())
                {
                    case DataType.Float:
                    {
                        SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                        OpenGLDebug.CheckError(SilkNetContext.GL, $"EnableVertexAttribArray({index})");
                        SilkNetContext.GL.VertexAttribPointer((uint)index,
                            element.Type.GetComponentCount(),
                            element.Type.ToBaseType().ToGLType(),
                            element.Normalized,
                            (uint)layout.Stride,
                            (void*)element.Offset);
                        OpenGLDebug.CheckError(SilkNetContext.GL, $"VertexAttribPointer({index})");
                    }
                        break;
                    case DataType.Int:
                    case DataType.UnsignedInt:
                    case DataType.Byte:
                    case DataType.UnsignedByte:
                    {
                        SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                        OpenGLDebug.CheckError(SilkNetContext.GL, $"EnableVertexAttribArray({index})");
                        SilkNetContext.GL.VertexAttribIPointer((uint)index,
                            element.Type.GetComponentCount(),
                            element.Type.ToBaseType().ToGLEnum(),
                            (uint)layout.Stride,
                            (void*)element.Offset);
                        OpenGLDebug.CheckError(SilkNetContext.GL, $"VertexAttribIPointer({index})");
                    }
                        break;
                    default:
                        throw new NotSupportedException($"BaseDataType {element.Type.ToBaseType()} not supported");
                }
            }
        }

        VertexBuffers.Add(vertexBuffer);
    }

    public void SetIndexBuffer(IIndexBuffer indexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(RendererId);
        indexBuffer.Bind();

        IndexBuffer = indexBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (RendererId != 0)
            {
                SilkNetContext.GL.DeleteVertexArray(RendererId);
                RendererId = 0;
            }

            foreach (var vertexBuffer in VertexBuffers)
                vertexBuffer?.Dispose();

            IndexBuffer?.Dispose();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL vertex array {RendererId}: {e.Message}");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLVertexArray()
    {
        if (!_disposed && RendererId != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: VertexArray {RendererId} not disposed! " +
                $"VBs: {VertexBuffers.Count}, IB: {(IndexBuffer != null ? "yes" : "no")}"
            );
        }
    }
#endif
}