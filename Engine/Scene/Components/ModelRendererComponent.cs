using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public Texture2D? OverrideTexture { get; set; } = null;
    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;
    
    public ModelRendererComponent()
    {
    }
    
    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone()
    {
        return new ModelRendererComponent
        {
            Color = Color,
            OverrideTexture = OverrideTexture,
            CastShadows = CastShadows,
            ReceiveShadows = ReceiveShadows
        };
    }
}