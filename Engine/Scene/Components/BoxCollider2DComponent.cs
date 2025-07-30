using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class BoxCollider2DComponent : IComponent
{
    public Vector2 Size { get; set; }
    public Vector2 Offset { get; set; }
    public float Density { get; set; }
    public float Friction { get; set; }
    public float Restitution { get; set; }
    public float RestitutionThreshold { get; set; }
    public bool IsTrigger { get; set; }

    public BoxCollider2DComponent()
    {
        Size = Vector2.Zero;
        Offset = new Vector2(0.5f, 0.5f);
        Density = 1.0f;
        Friction = 0.5f;
        Restitution = 0.0f;
        RestitutionThreshold = 0.5f;
        IsTrigger = false;
    }

    public BoxCollider2DComponent(Vector2 size, Vector2 offset, float density, float friction, float restitution, float restitutionThreshold, bool isTrigger)
    {
        Size = size;
        Offset = offset;
        Density = density;
        Friction = friction;
        Restitution = restitution;
        RestitutionThreshold = restitutionThreshold;
        IsTrigger = isTrigger;
    }
}