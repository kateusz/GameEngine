namespace Engine.Renderer.VertexArray;

/// <summary>
/// Factory interface for creating vertex array instances.
/// </summary>
public interface IVertexArrayFactory
{
    /// <summary>
    /// Creates a new vertex array.
    /// </summary>
    /// <returns>A vertex array instance.</returns>
    IVertexArray Create();
}
