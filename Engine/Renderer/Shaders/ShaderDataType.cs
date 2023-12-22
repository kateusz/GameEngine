using OpenTK.Graphics.OpenGL4;

namespace Engine.Renderer.Shaders;

public enum ShaderDataType
{
    None = 0, Float, Float2, Float3, Float4, Mat3, Mat4, Int, Int2, Int3, Int4, Bool
}

public static class ShaderDataTypeExtensions
{
    public static int GetSize(this ShaderDataType type)
    {
        return type switch
        {
            ShaderDataType.Float => 4,
            ShaderDataType.Float2 => 4 * 2,
            ShaderDataType.Float3 => 4 * 3,
            ShaderDataType.Float4 => 4 * 4,
            ShaderDataType.Mat3 => 4 * 3 * 3,
            ShaderDataType.Mat4 => 4 * 4 * 4,
            ShaderDataType.Int => 4,
            ShaderDataType.Int2 => 4 * 2,
            ShaderDataType.Int3 => 4 * 3,
            ShaderDataType.Int4 => 4 * 4,
            ShaderDataType.Bool => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static VertexAttribPointerType ToGLBaseType(this ShaderDataType type)
    {
        return type switch
        {
            ShaderDataType.Float => VertexAttribPointerType.Float,
            ShaderDataType.Float2 => VertexAttribPointerType.Float,
            ShaderDataType.Float3 => VertexAttribPointerType.Float,
            ShaderDataType.Float4 => VertexAttribPointerType.Float,
            ShaderDataType.Mat3 => VertexAttribPointerType.Float,
            ShaderDataType.Mat4 => VertexAttribPointerType.Float,
            ShaderDataType.Int => VertexAttribPointerType.Int,
            ShaderDataType.Int2 => VertexAttribPointerType.Int,
            ShaderDataType.Int3 => VertexAttribPointerType.Int,
            ShaderDataType.Int4 => VertexAttribPointerType.Int,
            // ShaderDataType.Bool => VertexAttribPointerType.Byte, ??
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}