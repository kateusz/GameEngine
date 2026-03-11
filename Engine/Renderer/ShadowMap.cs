namespace Engine.Renderer;

/// <summary>
/// Depth-only framebuffer for directional light shadow mapping.
/// </summary>
public interface IShadowMap : IDisposable
{
    uint Width { get; }
    uint Height { get; }
    uint DepthTextureId { get; }
    void Bind();
    void Unbind();
}
