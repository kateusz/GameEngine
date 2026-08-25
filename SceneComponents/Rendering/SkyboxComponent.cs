using ECS;

namespace SceneComponents.Rendering;

public class SkyboxComponent : IComponent
{
    public string? HdrPath { get; set; }
    public float Intensity { get; set; } = 1.0f;

    public IComponent Clone() => new SkyboxComponent
    {
        HdrPath = HdrPath,
        Intensity = Intensity
    };
}
