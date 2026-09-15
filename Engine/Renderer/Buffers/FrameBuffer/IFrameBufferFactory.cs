namespace Engine.Renderer.Buffers.FrameBuffer;

/// <summary>
/// Factory interface for creating framebuffer instances.
/// Framebuffers are not cached; the caller owns each instance and must dispose it.
/// </summary>
public interface IFrameBufferFactory
{
    /// <summary>
    /// Creates a new framebuffer with the default specification.
    /// The caller owns the result and must dispose it.
    /// </summary>
    /// <returns>A framebuffer instance.</returns>
    IFrameBuffer Create();
    /// <summary>
    /// Creates a new framebuffer with a custom specification.
    /// The caller owns the result and must dispose it.
    /// </summary>
    /// <param name="specification">Framebuffer configuration.</param>
    /// <returns>A framebuffer instance.</returns>
    IFrameBuffer Create(FrameBufferSpecification specification);
}
