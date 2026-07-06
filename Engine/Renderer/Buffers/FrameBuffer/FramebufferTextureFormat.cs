namespace Engine.Renderer.Buffers.FrameBuffer;

public enum FrameBufferTextureFormat
{
    None = 0,
    RGBA8 = 1,
    RGBA16F = 2,
    RED_INTEGER = 3,
    DEPTH24STENCIL8 = 4,
    Depth = DEPTH24STENCIL8
}
