namespace Engine.Renderer.Shaders;

public enum ShaderDataType
{
    None = 0, Float, Float2, Float3, Float4, Mat3, Mat4, Int, Int2, Int3, Int4, Bool
}

public static class ShaderDataTypeExtensions
{
    extension(ShaderDataType type)
    {
        public int GetSize()
        {
            return type switch
            {
                ShaderDataType.Float => RenderingConstants.FloatSize,
                ShaderDataType.Float2 => RenderingConstants.FloatSize * 2,
                ShaderDataType.Float3 => RenderingConstants.FloatSize * 3,
                ShaderDataType.Float4 => RenderingConstants.FloatSize * 4,
                ShaderDataType.Mat3 => RenderingConstants.FloatSize * 3 * 3,
                ShaderDataType.Mat4 => RenderingConstants.FloatSize * 4 * 4,
                ShaderDataType.Int => RenderingConstants.IntSize,
                ShaderDataType.Int2 => RenderingConstants.IntSize * 2,
                ShaderDataType.Int3 => RenderingConstants.IntSize * 3,
                ShaderDataType.Int4 => RenderingConstants.IntSize * 4,
                ShaderDataType.Bool => RenderingConstants.BoolSize,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public DataType ToBaseType()
        {
            return type switch
            {
                ShaderDataType.Float => DataType.Float,
                ShaderDataType.Float2 => DataType.Float,
                ShaderDataType.Float3 => DataType.Float,
                ShaderDataType.Float4 => DataType.Float,
                ShaderDataType.Mat3 => DataType.Float,
                ShaderDataType.Mat4 => DataType.Float,
                ShaderDataType.Int => DataType.Int,
                ShaderDataType.Int2 => DataType.Int,
                ShaderDataType.Int3 => DataType.Int,
                ShaderDataType.Int4 => DataType.Int,
                ShaderDataType.Bool => DataType.Byte,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public int GetComponentCount()
        {
            return type switch
            {
                ShaderDataType.Float => 1,
                ShaderDataType.Float2 => 2,
                ShaderDataType.Float3 => 3,
                ShaderDataType.Float4 => 4,
                ShaderDataType.Mat3 => 3 * 3,
                ShaderDataType.Mat4 => 4 * 4,
                ShaderDataType.Int => 1,
                ShaderDataType.Int2 => 2,
                ShaderDataType.Int3 => 3,
                ShaderDataType.Int4 => 4,
                ShaderDataType.Bool => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}