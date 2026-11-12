using Engine.Core.Window;

namespace Engine.Renderer.Pipeline;

/// <summary>
/// LOD selection uses framebuffer pixel height. Runtime uses the window;
/// editor Play overrides with the bound viewport framebuffer height.
/// </summary>
public sealed class LodViewportHeight(IGameWindow window)
{
    public float? OverridePixels { get; set; }

    public float Resolve() =>
        OverridePixels is > 0f ? OverridePixels.Value : window.FramebufferSize.Y;
}
