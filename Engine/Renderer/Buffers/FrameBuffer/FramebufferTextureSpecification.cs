namespace Engine.Renderer.Buffers.FrameBuffer;

public struct FrameBufferTextureSpecification
{
    public readonly FrameBufferTextureFormat TextureFormat = FrameBufferTextureFormat.None;

    public FrameBufferTextureSpecification(FrameBufferTextureFormat textureFormat)
    {
        TextureFormat = textureFormat;
    }
}
