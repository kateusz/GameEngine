namespace Engine.Renderer;

public interface IBloomRenderer : IDisposable
{
    void Resize(uint width, uint height);
    uint Apply(uint sourceColorTextureId, in BloomSettings settings);
}
