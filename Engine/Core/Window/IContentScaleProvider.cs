namespace Engine.Core.Window;

/// <summary>
/// Ratio of physical (framebuffer) pixels to logical (window) pixels.
/// Returns 2.0 on macOS Retina, 1.0 on standard displays.
/// </summary>
public interface IContentScaleProvider
{
    float ContentScale { get; }
}
