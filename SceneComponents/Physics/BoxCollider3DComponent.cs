using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class BoxCollider3DComponent : IComponent
{
    public Vector3 Size { get; set; } = new(0.5f, 0.5f, 0.5f);
    public Vector3 Offset { get; set; }
    public float Density { get; set; } = 1f;
    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.7f;
    public bool IsTrigger { get; set; }

    public IComponent Clone() => new BoxCollider3DComponent
    {
        Size = Size,
        Offset = Offset,
        Density = Density,
        Friction = Friction,
        Restitution = Restitution,
        IsTrigger = IsTrigger
    };
}
