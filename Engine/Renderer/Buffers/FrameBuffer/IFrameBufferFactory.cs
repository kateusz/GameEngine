namespace Engine.Renderer.Buffers.FrameBuffer;

/// <summary>
/// Factory interface for creating framebuffer instances.
/// </summary>
public interface IFrameBufferFactory
{
    /// <summary>
    /// Creates a new framebuffer with the specified specification.
    /// </summary>
    /// <returns>A framebuffer instance.</returns>
    IFrameBuffer Create();
}
