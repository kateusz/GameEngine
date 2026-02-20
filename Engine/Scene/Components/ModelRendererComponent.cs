using System.Numerics;
using System.Text.Json.Serialization;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    [JsonIgnore]
    public Texture2D? OverrideTexture { get; set; } = null;
    public string? OverrideTexturePath { get; set; }
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
            OverrideTexturePath = OverrideTexturePath,
            CastShadows = CastShadows,
            ReceiveShadows = ReceiveShadows
        };
    }
}