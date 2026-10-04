namespace Engine.Renderer.Textures;

public interface ISkyCapture : IDisposable
{
    bool GenerateEnvironmentMips();
    bool Begin(uint cubemap, int face, int mip, int size);
    uint IrradianceId { get; }
    uint PrefilterId { get; }
}
