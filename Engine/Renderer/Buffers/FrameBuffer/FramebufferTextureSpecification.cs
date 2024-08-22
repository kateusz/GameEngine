namespace Engine.Renderer.Buffers.FrameBuffer;

public struct FramebufferTextureSpecification
{
    public FramebufferTextureFormat TextureFormat = FramebufferTextureFormat.None;

    public FramebufferTextureSpecification(FramebufferTextureFormat textureFormat)
    {
        TextureFormat = textureFormat;
    }
}