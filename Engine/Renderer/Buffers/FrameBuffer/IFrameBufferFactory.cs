namespace Engine.Renderer.Buffers.FrameBuffer;

/// <summary>
/// Factory interface for creating framebuffer instances.
/// </summary>
public interface IFrameBufferFactory
{
    /// <summary>
    /// Creates a new framebuffer with the default specification.
    /// </summary>
    /// <returns>A framebuffer instance.</returns>
    IFrameBuffer Create();
    /// <summary>
    /// Creates a new framebuffer with a custom specification.
    /// </summary>
    /// <param name="specification">Framebuffer configuration.</param>
    /// <returns>A framebuffer instance.</returns>
    IFrameBuffer Create(FrameBufferSpecification specification);
}
