using Engine.Renderer.Shaders;

namespace Engine.Renderer.Buffers;

public record struct BufferElement(string Name, ShaderDataType Type, int Size, int Offset, bool Normalized)
{
    public BufferElement(ShaderDataType type, string name, bool normalized = false)
        : this(name, type, type.GetSize(), 0, normalized) { }
}