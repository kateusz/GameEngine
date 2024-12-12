using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SubTextureRendererComponent : Component
{
    public Vector2 Coords { get; set; }
    public Texture2D? Texture { get; set; }
}