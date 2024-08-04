using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SpriteRendererComponent(Vector4 color) : Component
{
    public Vector4 Color { get; set; } = color;
}