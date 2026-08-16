using ECS;

namespace SceneComponents.Lighting;

public class SkyLightComponent : IComponent
{
    /// <summary>Project-relative path to an equirectangular radiance .hdr.</summary>
    public string HdrPath { get; set; } = string.Empty;

    public float Intensity { get; set; } = 1.0f;

    public IComponent Clone()
    {
        return new SkyLightComponent
        {
            HdrPath = HdrPath,
            Intensity = Intensity
        };
    }
}
