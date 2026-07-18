using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class BufferLayoutTests
{
    [Fact]
    public void Constructor_CalculatesStride_AsSumOfElementSizes()
    {
        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color")
        ]);

        layout.Stride.ShouldBe(12 + 16);
    }

    [Fact]
    public void Constructor_AssignsOffsets_SequentiallyFromZero()
    {
        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);

        layout.Elements[0].Offset.ShouldBe(0);
        layout.Elements[1].Offset.ShouldBe(12);
        layout.Elements[2].Offset.ShouldBe(28);
        layout.Stride.ShouldBe(32);
    }

    [Fact]
    public void Constructor_WithMultipleElements_ProducesCorrectStrideForQuadLikeLayout()
    {
        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float, "a_TexIndex"),
            new BufferElement(ShaderDataType.Float, "a_TilingFactor"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);

        layout.Stride.ShouldBe(48);
        layout.Elements[0].Offset.ShouldBe(0);
        layout.Elements[1].Offset.ShouldBe(12);
        layout.Elements[2].Offset.ShouldBe(28);
        layout.Elements[3].Offset.ShouldBe(36);
        layout.Elements[4].Offset.ShouldBe(40);
        layout.Elements[5].Offset.ShouldBe(44);
    }
}
