namespace Engine.Renderer.Buffers.FrameBuffer;

public enum FramebufferTextureFormat
{
    None = 0,

    // Color
    RGBA8,

    // Depth/stencil
    DEPTH24STENCIL8,

    // Defaults
    Depth = DEPTH24STENCIL8
}