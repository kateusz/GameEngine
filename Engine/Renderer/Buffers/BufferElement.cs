using Engine.Renderer.Shaders;

namespace Engine.Renderer.Buffers;

public record struct BufferElement(string Name, ShaderDataType Type, int Size, int Offset, bool Normalized)
{
    // Keep the computed constructor as a convenience method
    public BufferElement(ShaderDataType type, string name, bool normalized = false)
        : this(name, type, type.GetSize(), 0, normalized) { }

    public int GetComponentCount()
    {
        return Type switch
        {
            ShaderDataType.Float => 1,
            ShaderDataType.Float2 => 2,
            ShaderDataType.Float3 => 3,
            ShaderDataType.Float4 => 4,
            ShaderDataType.Mat3 => 3,
            ShaderDataType.Mat4 => 4,
            ShaderDataType.Int => 1,
            ShaderDataType.Int2 => 2,
            ShaderDataType.Int3 => 3,
            ShaderDataType.Int4 => 4,
            ShaderDataType.Bool => 1,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}