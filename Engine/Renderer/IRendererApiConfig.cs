namespace Engine.Renderer;

/// <summary>
/// Configuration for the renderer API type.
/// This interface replaces the static RendererApiType class to enable proper dependency injection.
/// </summary>
public interface IRendererApiConfig
{
    /// <summary>
    /// Gets the renderer API type to use for creating graphics resources.
    /// </summary>
    ApiType Type { get; }
}
