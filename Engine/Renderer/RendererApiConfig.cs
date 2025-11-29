namespace Engine.Renderer;

/// <summary>
/// Default implementation of IRendererApiConfig.
/// Provides configuration for which renderer API to use (SilkNet/OpenGL).
/// </summary>
internal sealed class RendererApiConfig(ApiType type) : IRendererApiConfig
{
    /// <summary>
    /// Gets the renderer API type.
    /// </summary>
    public ApiType Type { get; } = type;
}
