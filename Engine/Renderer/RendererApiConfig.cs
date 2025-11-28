namespace Engine.Renderer;

/// <summary>
/// Default implementation of IRendererApiConfig.
/// Provides configuration for which renderer API to use (SilkNet/OpenGL).
/// </summary>
public class RendererApiConfig : IRendererApiConfig
{
    /// <summary>
    /// Gets the renderer API type.
    /// </summary>
    public ApiType Type { get; }

    /// <summary>
    /// Initializes a new instance of the RendererApiConfig class.
    /// </summary>
    /// <param name="type">The renderer API type to use.</param>
    public RendererApiConfig(ApiType type)
    {
        Type = type;
    }
}
