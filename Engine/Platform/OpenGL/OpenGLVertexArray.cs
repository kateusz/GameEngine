using System.Diagnostics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLVertexArray : IVertexArray
{
    private readonly uint _vertexArrayObject;
    private bool _disposed;

    public OpenGLVertexArray()
    {
        _vertexArrayObject = SilkNetContext.GL.GenVertexArray();
        OpenGLDebug.CheckError(SilkNetContext.GL, "GenVertexArray");
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindVertexArray");
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindVertexArray(0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UnbindVertexArray");
    }

    public void AddVertexBuffer(IVertexBuffer vertexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);

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
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
        indexBuffer.Bind();

        IndexBuffer = indexBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_vertexArrayObject != 0)
                SilkNetContext.GL.DeleteVertexArray(_vertexArrayObject);

            foreach (var vertexBuffer in VertexBuffers)
                vertexBuffer?.Dispose();

            IndexBuffer?.Dispose();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL vertex array {_vertexArrayObject}: {e.Message}");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLVertexArray()
    {
        if (!_disposed && _vertexArrayObject != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: VertexArray {_vertexArrayObject} not disposed! " +
                $"VBs: {VertexBuffers.Count}, IB: {(IndexBuffer != null ? "yes" : "no")}"
            );
        }
    }
#endif
}