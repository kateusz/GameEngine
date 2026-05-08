using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public List<MaterialData> Materials { get; set; } = [];
    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone()
    {
        return new ModelRendererComponent
        {
            Color = Color,
            Materials = [..Materials],
            CastShadows = CastShadows,
            ReceiveShadows = ReceiveShadows
        };
    }
}
