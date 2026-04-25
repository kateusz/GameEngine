namespace Engine.Renderer.Buffers.FrameBuffer;

public enum FramebufferTextureFormat
{
    None = 0,

    // Color
    RGBA8 = 1,
    
    RGBA16F = 2,
    
    RED_INTEGER = 3,

    // Depth/stencil
    DEPTH24STENCIL8 = 4,
    DEPTH32F = 5,

    // Defaults
    Depth = DEPTH24STENCIL8
}