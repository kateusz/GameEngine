using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;

namespace Engine.GraphicsTests;

internal static class TestBufferLayouts
{
    public static BufferLayout FloatOnly { get; } = new([
        new BufferElement(ShaderDataType.Float3, "a_Position"),
        new BufferElement(ShaderDataType.Float4, "a_Color")
    ]);

    public static int FloatOnlyStride => 28;

    public static BufferLayout MixedFloatInt { get; } = new([
        new BufferElement(ShaderDataType.Float3, "a_Position"),
        new BufferElement(ShaderDataType.Int, "a_EntityID")
    ]);

    public static int MixedFloatIntStride => 16;
}
