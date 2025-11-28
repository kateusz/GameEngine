namespace Engine.Renderer.Buffers.FrameBuffer;

public enum FramebufferTextureFormat
{
    None = 0,

    // Color
    RGBA8 = 1,
    
    RED_INTEGER = 2,

    // Depth/stencil
    DEPTH24STENCIL8 = 3,

    // Defaults
    Depth = DEPTH24STENCIL8
}