namespace Engine.Renderer.Buffers.FrameBuffer;

public enum FrameBufferTextureFilter
{
    Nearest = 0,
    Linear = 1
}

public enum FrameBufferTextureWrap
{
    Repeat = 0,
    ClampToEdge = 1,
    ClampToBorder = 2
}

public struct FrameBufferTextureSpecification
{
    public readonly FrameBufferTextureFormat TextureFormat = FrameBufferTextureFormat.None;
    public FrameBufferTextureFilter Filter { get; init; } = FrameBufferTextureFilter.Nearest;
    public FrameBufferTextureWrap Wrap { get; init; } = FrameBufferTextureWrap.Repeat;

    public FrameBufferTextureSpecification(FrameBufferTextureFormat textureFormat)
    {
        TextureFormat = textureFormat;
    }
}
