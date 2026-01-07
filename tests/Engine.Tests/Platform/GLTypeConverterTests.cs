using Engine.Platform.OpenGL;
using Engine.Renderer.Shaders;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.Tests.Platform;

public class GLTypeConverterTests
{
    [Theory]
    [InlineData(DataType.Float, VertexAttribPointerType.Float)]
    [InlineData(DataType.Int, VertexAttribPointerType.Int)]
    [InlineData(DataType.UnsignedInt, VertexAttribPointerType.UnsignedInt)]
    [InlineData(DataType.Byte, VertexAttribPointerType.Byte)]
    [InlineData(DataType.UnsignedByte, VertexAttribPointerType.UnsignedByte)]
    public void ToGLType_ReturnsCorrectVertexAttribPointerType(DataType type, VertexAttribPointerType expected)
    {
        // Act
        var result = type.ToGLType();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(DataType.Float, GLEnum.Float)]
    [InlineData(DataType.Int, GLEnum.Int)]
    [InlineData(DataType.UnsignedInt, GLEnum.UnsignedInt)]
    [InlineData(DataType.Byte, GLEnum.Byte)]
    [InlineData(DataType.UnsignedByte, GLEnum.UnsignedByte)]
    public void ToGLEnum_ReturnsCorrectGLEnum(DataType type, GLEnum expected)
    {
        // Act
        var result = type.ToGLEnum();

        // Assert
        result.ShouldBe(expected);
    }
}
