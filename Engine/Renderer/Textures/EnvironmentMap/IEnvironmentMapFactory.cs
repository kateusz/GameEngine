namespace Engine.Renderer.Textures.EnvironmentMap;

/// <summary>
/// Generates and caches IBL environment maps plus shared IBL resources (BRDF LUT, neutral fallback cubemap).
/// GetOrCreate caches failures (null) — errors log once.
/// </summary>
public interface IEnvironmentMapFactory
{
    EnvironmentMap? GetOrCreate(string resolvedHdrPath);
    Texture2D GetBrdfLut();
    TextureCube GetBlackCubemap();
}
