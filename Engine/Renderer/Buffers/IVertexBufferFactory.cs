namespace Engine.Renderer.Buffers;

/// <summary>
/// Factory interface for creating vertex buffer instances.
/// </summary>
public interface IVertexBufferFactory
{
    /// <summary>
    /// Creates a new vertex buffer with the specified size.
    /// </summary>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>A vertex buffer instance.</returns>
    IVertexBuffer Create(uint size);
}
