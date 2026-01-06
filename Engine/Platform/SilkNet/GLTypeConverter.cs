using Silk.NET.OpenGL;
using Engine.Renderer.Shaders;

namespace Engine.Platform.SilkNet;

/// <summary>
/// Extension methods for converting platform-agnostic types to OpenGL-specific types
/// </summary>
internal static class DataTypeExtensions
{
    extension(DataType type)
    {
        public VertexAttribPointerType ToGLType()
        {
            return type switch
            {
                DataType.Float => VertexAttribPointerType.Float,
                DataType.Int => VertexAttribPointerType.Int,
                DataType.UnsignedInt => VertexAttribPointerType.UnsignedInt,
                DataType.Byte => VertexAttribPointerType.Byte,
                DataType.UnsignedByte => VertexAttribPointerType.UnsignedByte,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public GLEnum ToGLEnum()
        {
            return type switch
            {
                DataType.Float => GLEnum.Float,
                DataType.Int => GLEnum.Int,
                DataType.UnsignedInt => GLEnum.UnsignedInt,
                DataType.Byte => GLEnum.Byte,
                DataType.UnsignedByte => GLEnum.UnsignedByte,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
