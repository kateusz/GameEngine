using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class BoxCollider2DComponent : IComponent
{
    private float _density;
    private float _friction;
    private float _restitution;

    public Vector2 Size { get; set; }
    public Vector2 Offset { get; set; }

    public float Density
    {
        get => _density;
        set
        {
            if (!_density.Equals(value))
            {
                _density = value;
                IsDirty = true;
            }
        }
    }

    public float Friction
    {
        get => _friction;
        set
        {
            if (!_friction.Equals(value))
            {
                _friction = value;
                IsDirty = true;
            }
        }
    }

    public float Restitution
    {
        get => _restitution;
        set
        {
            if (!_restitution.Equals(value))
            {
                _restitution = value;
                IsDirty = true;
            }
        }
    }

    public float RestitutionThreshold { get; set; }
    public bool IsTrigger { get; set; }

    /// <summary>
    /// Indicates whether fixture properties (Density, Friction, Restitution) have been modified
    /// and need to be synchronized with the Box2D fixture.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Clears the dirty flag after fixture properties have been synchronized.
    /// </summary>
    public void ClearDirtyFlag() => IsDirty = false;

    public BoxCollider2DComponent()
    {
        Size = Vector2.Zero;
        Offset = Vector2.Zero;
        _density = 1.0f;
        _friction = 0.5f;
        _restitution = 0.0f;
        RestitutionThreshold = 0.5f;
        IsTrigger = false;
        IsDirty = true; // Initially dirty to ensure first-time setup
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
        IsDirty = true; // Initially dirty to ensure first-time setup
    }
}