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

/// <summary>
/// Texture minification filter modes for framebuffer attachments.
/// Controls how textures are sampled when appearing smaller than their original size.
/// </summary>
public enum TextureMinFilter
{
    /// <summary>
    /// Linear interpolation between texels - produces smooth results.
    /// </summary>
    Linear,

    /// <summary>
    /// Nearest-neighbor sampling - produces sharp, pixelated results.
    /// Ideal for pixel-art or when exact values are needed (e.g., entity IDs).
    /// </summary>
    Nearest,

    /// <summary>
    /// Linear interpolation with mipmaps using linear interpolation between mipmap levels.
    /// Provides best quality for textures with mipmaps.
    /// </summary>
    LinearMipmapLinear,

    /// <summary>
    /// Nearest-neighbor sampling with mipmaps using nearest mipmap level.
    /// </summary>
    NearestMipmapNearest
}

/// <summary>
/// Texture magnification filter modes for framebuffer attachments.
/// Controls how textures are sampled when appearing larger than their original size.
/// </summary>
public enum TextureMagFilter
{
    /// <summary>
    /// Linear interpolation between texels - produces smooth results.
    /// </summary>
    Linear,

    /// <summary>
    /// Nearest-neighbor sampling - produces sharp, pixelated results.
    /// Ideal for pixel-art or when exact values are needed (e.g., entity IDs).
    /// </summary>
    Nearest
}

public struct FramebufferTextureSpecification
{
    public FramebufferTextureFormat TextureFormat = FramebufferTextureFormat.None;

    /// <summary>
    /// Texture minification filter applied when texture appears smaller than screen space.
    /// Defaults to Linear for smooth rendering.
    /// </summary>
    public TextureMinFilter MinFilter = TextureMinFilter.Linear;

    /// <summary>
    /// Texture magnification filter applied when texture appears larger than screen space.
    /// Defaults to Linear for smooth rendering.
    /// </summary>
    public TextureMagFilter MagFilter = TextureMagFilter.Linear;

    public FramebufferTextureSpecification(
        FramebufferTextureFormat textureFormat,
        TextureMinFilter minFilter = TextureMinFilter.Linear,
        TextureMagFilter magFilter = TextureMagFilter.Linear)
    {
        TextureFormat = textureFormat;
        MinFilter = minFilter;
        MagFilter = magFilter;
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
        width = this.Width;
        height = this.Height;
        samples = this.Samples;
        swapChainTarget = this.SwapChainTarget;
    }
}