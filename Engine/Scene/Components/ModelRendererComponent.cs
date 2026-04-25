using System.Numerics;
using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public List<PbrMaterial> Materials { get; set; } = [];
    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone() => new ModelRendererComponent
    {
        Color = Color,
        Materials = [..Materials],
        CastShadows = CastShadows,
        ReceiveShadows = ReceiveShadows
    };
}
