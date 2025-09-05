namespace Engine.Renderer.Buffers.FrameBuffer;
public class FramebufferAttachmentSpecification
{
    public List<FramebufferTextureSpecification> Attachments;

    public FramebufferAttachmentSpecification(List<FramebufferTextureSpecification> attachments)
    {
        Attachments = attachments;
    }
}

public enum FramebufferTextureFormat
{
    None = 0,

    // Color
    RGBA8,
    
    RED_INTEGER,

    // Depth/stencil
    DEPTH24STENCIL8,

    // Defaults
    Depth = DEPTH24STENCIL8
}

public struct FramebufferTextureSpecification
{
    public FramebufferTextureFormat TextureFormat = FramebufferTextureFormat.None;

    public FramebufferTextureSpecification(FramebufferTextureFormat textureFormat)
    {
        TextureFormat = textureFormat;
    }
}

public class FrameBufferSpecification(uint width, uint height, uint samples = 1, bool swapChainTarget = false)
{
    public uint Width { get;  set; } = width;
    public uint Height { get;  set; } = height;
    public uint Samples { get; init; } = samples;
    public bool SwapChainTarget { get; init; } = swapChainTarget;
    public FramebufferAttachmentSpecification AttachmentsSpec { get; set; }

    public void Deconstruct(out uint width, out uint height, out uint samples, out bool swapChainTarget)
    {
        width = Width;
        height = Height;
        samples = Samples;
        swapChainTarget = SwapChainTarget;
    }
}