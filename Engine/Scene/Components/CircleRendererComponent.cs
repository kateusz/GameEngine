using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class CircleRendererComponent : Component
{
    public Vector4 Color { get; set; }
    public float Thickness { get; set; } = 1.0f;
    public float Fade { get; set; } = 0.5f;
}