namespace Engine.Renderer.Buffers;

/// <summary>
/// Factory interface for creating index buffer instances.
/// </summary>
public interface IIndexBufferFactory
{
    /// <summary>
    /// Creates a new index buffer with the specified indices.
    /// </summary>
    /// <param name="indices">The index data.</param>
    /// <param name="count">The number of indices.</param>
    /// <returns>An index buffer instance.</returns>
    IIndexBuffer Create(uint[] indices, int count);
}
