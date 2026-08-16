using Engine.Renderer.Textures;

namespace Engine.Renderer;

/// <summary>Bakes and caches IBL environment maps. GetOrCreate caches failures (null) — errors log once.</summary>
public interface IEnvironmentMapFactory
{
    EnvironmentMap? GetOrCreate(string resolvedHdrPath);
    uint GetBrdfLutId();
    TextureCube GetBlackCubemap();
}
