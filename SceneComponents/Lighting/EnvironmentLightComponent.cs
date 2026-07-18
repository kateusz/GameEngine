using ECS;

namespace SceneComponents.Lighting;

/// <summary>
/// Scene-wide HDR environment probe. Loads .hdr; draws as skybox when present.
/// </summary>
public class EnvironmentLightComponent : IComponent
{
    /// <summary>Asset-relative or absolute path to a Radiance .hdr equirectangular map.</summary>
    public string? HdrPath { get; set; }

    /// <summary>Skybox exposure multiplier before Reinhard display map.</summary>
    public float Exposure { get; set; } = 1.0f;

    public IComponent Clone() => new EnvironmentLightComponent
    {
        HdrPath = HdrPath,
        Exposure = Exposure
    };
}
