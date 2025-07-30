using Engine.Renderer.Shaders;

namespace Engine.Renderer.Buffers;

public record struct BufferElement(string Name, ShaderDataType Type, int Size, int Offset, bool Normalized)
{
    // Keep the computed constructor as a convenience method
    public BufferElement(ShaderDataType type, string name, bool normalized = false)
        : this(name, type, type.GetSize(), 0, normalized) { }

    public int GetComponentCount()
    {
        switch (Type)
        {
            case ShaderDataType.Float:
                return 1;
            case ShaderDataType.Float2:
                return 2;
            case ShaderDataType.Float3:
                return 3;
            case ShaderDataType.Float4:
                return 4;
            case ShaderDataType.Mat3:
                return 3;
            case ShaderDataType.Mat4:
                return 4;
            case ShaderDataType.Int:
                return 1;
            case ShaderDataType.Int2:
                return 2;
            case ShaderDataType.Int3:
                return 3;
            case ShaderDataType.Int4:
                return 4;
            case ShaderDataType.Bool:
                return 1;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}