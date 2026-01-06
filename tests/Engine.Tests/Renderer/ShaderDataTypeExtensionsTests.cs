using Engine.Renderer.Shaders;
using Shouldly;

namespace Engine.Tests.Renderer;

public class ShaderDataTypeExtensionsTests
{
    [Theory]
    [InlineData(ShaderDataType.Float, DataType.Float)]
    [InlineData(ShaderDataType.Float2, DataType.Float)]
    [InlineData(ShaderDataType.Float3, DataType.Float)]
    [InlineData(ShaderDataType.Float4, DataType.Float)]
    [InlineData(ShaderDataType.Mat3, DataType.Float)]
    [InlineData(ShaderDataType.Mat4, DataType.Float)]
    [InlineData(ShaderDataType.Int, DataType.Int)]
    [InlineData(ShaderDataType.Int2, DataType.Int)]
    [InlineData(ShaderDataType.Int3, DataType.Int)]
    [InlineData(ShaderDataType.Int4, DataType.Int)]
    [InlineData(ShaderDataType.Bool, DataType.Byte)]
    public void ToBaseType_ReturnsCorrectBaseType(ShaderDataType shaderType, DataType expectedType)
    {
        // Act
        var result = shaderType.ToBaseType();

        // Assert
        result.ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(ShaderDataType.Float, 1)]
    [InlineData(ShaderDataType.Float2, 2)]
    [InlineData(ShaderDataType.Float3, 3)]
    [InlineData(ShaderDataType.Float4, 4)]
    [InlineData(ShaderDataType.Mat3, 9)]
    [InlineData(ShaderDataType.Mat4, 16)]
    [InlineData(ShaderDataType.Int, 1)]
    [InlineData(ShaderDataType.Int2, 2)]
    [InlineData(ShaderDataType.Int3, 3)]
    [InlineData(ShaderDataType.Int4, 4)]
    [InlineData(ShaderDataType.Bool, 1)]
    public void GetComponentCount_ReturnsCorrectCount(ShaderDataType shaderType, int expectedCount)
    {
        // Act
        var result = shaderType.GetComponentCount();

        // Assert
        result.ShouldBe(expectedCount);
    }
}
