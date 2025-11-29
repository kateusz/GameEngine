namespace Engine.Renderer.Buffers.FrameBuffer;

public struct FramebufferTextureSpecification
{
    public readonly FramebufferTextureFormat TextureFormat = FramebufferTextureFormat.None;

    public FramebufferTextureSpecification(FramebufferTextureFormat textureFormat)
    {
        TextureFormat = textureFormat;
    }
}