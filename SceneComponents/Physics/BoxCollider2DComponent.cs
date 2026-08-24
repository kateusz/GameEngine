using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class BoxCollider2DComponent : IComponent
{
    private float _density;
    private float _friction;
    private float _restitution;

    public Vector2 Size
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    } = new(0.5f, 0.5f);

    public Vector2 Offset
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    }

    public float Density
    {
        get => _density;
        set
        {
            if (_density.Equals(value))
                return;
            _density = value;
            PhysicsBodyRevision.Bump();
        }
    }

    public float Friction
    {
        get => _friction;
        set => _friction = value;
    }

    public float Restitution
    {
        get => _restitution;
        set => _restitution = value;
    }

    public float RestitutionThreshold { get; set; }

    public bool IsTrigger
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    }

    public BoxCollider2DComponent()
    {
        Offset = Vector2.Zero;
        _density = 1.0f;
        _friction = 0.5f;
        _restitution = 0.7f;
        RestitutionThreshold = 0.5f;
    }

    public BoxCollider2DComponent(Vector2 size, Vector2 offset, float density, float friction, float restitution, float restitutionThreshold, bool isTrigger)
    {
        Size = size;
        Offset = offset;
        _density = density;
        _friction = friction;
        _restitution = restitution;
        RestitutionThreshold = restitutionThreshold;
        IsTrigger = isTrigger;
    }

    public IComponent Clone()
    {
        return new BoxCollider2DComponent(Size, Offset, _density, _friction, _restitution, RestitutionThreshold, IsTrigger);
    }
}
