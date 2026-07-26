using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class EdgeCollider2DComponent : IComponent
{
    public List<Vector2> Points { get; set; } = [new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f)];
    public float Density { get; set; } = 1.0f;
    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.7f;
    public bool IsTrigger { get; set; }

    public IComponent Clone() => new EdgeCollider2DComponent
    {
        Points = [.. Points],
        Density = Density,
        Friction = Friction,
        Restitution = Restitution,
        IsTrigger = IsTrigger
    };
}
