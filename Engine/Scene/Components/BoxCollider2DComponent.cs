using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public record struct BoxCollider2DComponent(
    Vector2 Size, 
    Vector2 Offset, 
    float Density, 
    float Friction, 
    float Restitution, 
    float RestitutionThreshold, 
    bool IsTrigger) : IComponent
{
    public BoxCollider2DComponent() : this(
        Vector2.Zero, 
        new Vector2(0.5f, 0.5f), 
        1.0f, 
        0.5f, 
        0.0f, 
        0.5f, 
        false) { }
}