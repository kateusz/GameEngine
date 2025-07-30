using Engine.Renderer.Shaders;

namespace Engine.Renderer.Buffers;

public struct BufferElement
{
    public string Name { get; set; }
    public ShaderDataType Type { get; set; }
    public int Size { get; set; }
    public int Offset { get; set; }
    public bool Normalized { get; set; }

    public BufferElement(ShaderDataType type, string name, bool normalized = false)
    {
        Type = type;
        Name = name;
        Normalized = normalized;
        Offset = 0;
        Size = type.GetSize();
    }

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