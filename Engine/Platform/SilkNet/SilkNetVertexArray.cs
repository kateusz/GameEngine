using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetVertexArray : IVertexArray, IDisposable
{
    private readonly uint _vertexArrayObject;
    private bool _disposed = false;

    public SilkNetVertexArray()
    {
        _vertexArrayObject = SilkNetContext.GL.GenVertexArray();
        VertexBuffers = new List<IVertexBuffer>();
    }

    public IList<IVertexBuffer> VertexBuffers { get; private set; }

    public IIndexBuffer IndexBuffer { get; private set; }

    public void Bind()
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindVertexArray(0);
    }

    public void AddVertexBuffer(IVertexBuffer vertexBuffer)
    {
        SilkNetContext.GL.BindVertexArray(_vertexArrayObject);

        vertexBuffer.Bind();

        if (vertexBuffer.Layout is null)
        {
            throw new InvalidOperationException("Vertex buffer has no layout!");
        }

        var layout = vertexBuffer.Layout.Value; // Access the struct value

        for (int index = 0; index < layout.Elements.Count; index++)
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
                        SilkNetContext.GL.VertexAttribPointer((uint)index,
                            element.GetComponentCount(),
                            ShaderDataTypeToOpenGLBaseType(element.Type),
                            element.Normalized,
                            (uint)layout.Stride,
                            (void*)element.Offset); // what about element.Offset + sizeof(float) * count * i)???
                    }
                        break;
                    case ShaderDataType.Int:
                    case ShaderDataType.Int2:
                    case ShaderDataType.Int3:
                    case ShaderDataType.Int4:
                    case ShaderDataType.Bool:
                    {
                        SilkNetContext.GL.EnableVertexAttribArray((uint)index);
                        SilkNetContext.GL.VertexAttribIPointer((uint)index,
                            element.GetComponentCount(),
                            ShaderDataTypeToOpenGLBaseType(element.Type),
                            (uint)layout.Stride,
                            (void*)element.Offset);
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
        switch (type)
        {
            case ShaderDataType.Float: return GLEnum.Float;
            case ShaderDataType.Float2: return GLEnum.Float;
            case ShaderDataType.Float3: return GLEnum.Float;
            case ShaderDataType.Float4: return GLEnum.Float;
            case ShaderDataType.Mat3: return GLEnum.Float;
            case ShaderDataType.Mat4: return GLEnum.Float;
            case ShaderDataType.Int: return GLEnum.Int;
            case ShaderDataType.Int2: return GLEnum.Int;
            case ShaderDataType.Int3: return GLEnum.Int;
            case ShaderDataType.Int4: return GLEnum.Int;
            case ShaderDataType.Bool: return GLEnum.Bool;
        }

        return 0;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                VertexBuffers?.Clear();
                IndexBuffer = null;
            }

            // Delete the VAO only during disposal
            if (_vertexArrayObject != 0)
            {
                SilkNetContext.GL.DeleteVertexArray(_vertexArrayObject);
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SilkNetVertexArray()
    {
        Dispose(false);
    }
}