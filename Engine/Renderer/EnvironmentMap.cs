using Engine.Renderer.Textures;

namespace Engine.Renderer;

/// <summary>GPU resources for one baked environment: skybox, diffuse irradiance, GGX-prefiltered specular.</summary>
public sealed class EnvironmentMap(TextureCube environment, TextureCube irradiance, TextureCube prefiltered)
    : IDisposable
{
    public const float MaxReflectionLod = 4f;

    public TextureCube Environment { get; } = environment;
    public TextureCube Irradiance { get; } = irradiance;
    public TextureCube Prefiltered { get; } = prefiltered;

    public void Dispose()
    {
        Environment.Dispose();
        Irradiance.Dispose();
        Prefiltered.Dispose();
    }
}
