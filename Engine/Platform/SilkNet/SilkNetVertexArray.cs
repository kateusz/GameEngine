using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

internal sealed class SilkNetVertexArray : IVertexArray
{
    private readonly uint _vertexArrayObject;
    private bool _disposed;

    public SilkNetVertexArray()
    {
        _vertexArrayObject = SilkNetContext.GL.GenVertexArray();
        GLDebug.CheckError(SilkNetContext.GL, "GenVertexArray");
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
        GLDebug.CheckError(SilkNetContext.GL, "BindVertexArray");
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindVertexArray(0);
        GLDebug.CheckError(SilkNetContext.GL, "UnbindVertexArray");
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

                switch (element.Type)
                {
                    case ShaderDataType.Float:
                    case ShaderDataType.Float2:
                    case ShaderDataType.Float3:
                    case ShaderDataType.Float4:
                    {
                        SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                        GLDebug.CheckError(SilkNetContext.GL, $"EnableVertexAttribArray({index})");
                        SilkNetContext.GL.VertexAttribPointer((uint)index,
                            element.GetComponentCount(),
                            ShaderDataTypeToOpenGLBaseType(element.Type),
                            element.Normalized,
                            (uint)layout.Stride,
                            (void*)element.Offset);
                        GLDebug.CheckError(SilkNetContext.GL, $"VertexAttribPointer({index})");
                    }
                        break;
                    case ShaderDataType.Int:
                    case ShaderDataType.Int2:
                    case ShaderDataType.Int3:
                    case ShaderDataType.Int4:
                    case ShaderDataType.Bool:
                    {
                        SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                        GLDebug.CheckError(SilkNetContext.GL, $"EnableVertexAttribArray({index})");
                        SilkNetContext.GL.VertexAttribIPointer((uint)index,
                            element.GetComponentCount(),
                            ShaderDataTypeToOpenGLBaseType(element.Type),
                            (uint)layout.Stride,
                            (void*)element.Offset);
                        GLDebug.CheckError(SilkNetContext.GL, $"VertexAttribIPointer({index})");
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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

    private GLEnum ShaderDataTypeToOpenGLBaseType(ShaderDataType type)
    {
        return type switch
        {
            ShaderDataType.Float => GLEnum.Float,
            ShaderDataType.Float2 => GLEnum.Float,
            ShaderDataType.Float3 => GLEnum.Float,
            ShaderDataType.Float4 => GLEnum.Float,
            ShaderDataType.Mat3 => GLEnum.Float,
            ShaderDataType.Mat4 => GLEnum.Float,
            ShaderDataType.Int => GLEnum.Int,
            ShaderDataType.Int2 => GLEnum.Int,
            ShaderDataType.Int3 => GLEnum.Int,
            ShaderDataType.Int4 => GLEnum.Int,
            ShaderDataType.Bool => GLEnum.Bool,
            _ => 0
        };
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
    }
}