using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class CircleCollider2DComponent : Component
{
    public Vector2 Offset { get; set; } = new Vector2(0.5f, 0.5f);
    public float Radius { get; set; } = 0.5f;
    public float Density { get; set; } = 1.0f;
    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.0f;
    public float RestitutionThreshold { get; set; } = 0.5f;
}