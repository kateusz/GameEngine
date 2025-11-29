namespace Engine.Renderer;

/// <summary>
/// Factory interface for creating renderer API instances.
/// </summary>
public interface IRendererApiFactory
{
    /// <summary>
    /// Creates a new instance of the configured renderer API.
    /// </summary>
    /// <returns>A renderer API instance.</returns>
    IRendererAPI Create();
}
