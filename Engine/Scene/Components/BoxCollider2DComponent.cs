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

    /// <summary>
    /// The density of the collider in kg/m².
    /// Higher density = heavier object. Typical values: 0.1 to 10.0.
    /// Default is 1.0 (similar to water).
    /// </summary>
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

    /// <summary>
    /// The friction coefficient (0-1).
    /// 0 = ice (no friction), 1 = very rough surface.
    /// Typical values: 0.2-0.8. Default is 0.5.
    /// </summary>
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

    /// <summary>
    /// The restitution (bounciness) coefficient (0-1).
    /// 0 = no bounce (inelastic), 1 = perfect bounce (elastic).
    /// Typical values: 0.0-0.5. Default is 0.0 (no bounce).
    /// </summary>
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

    /// <summary>
    /// The restitution threshold velocity in m/s.
    /// Collisions below this velocity will not bounce regardless of restitution value.
    /// Prevents jittering at rest. Typical values: 0.5 to 1.0 m/s.
    /// Default is 0.5 m/s.
    /// </summary>
    public float RestitutionThreshold { get; set; }
    /// <summary>
    /// Whether this collider acts as a trigger (sensor).
    /// Triggers detect collisions but don't cause physical response.
    /// Useful for pickups, checkpoint zones, and other non-physical interactions.
    /// Default is false (normal collision).
    /// </summary>
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

    public IComponent Clone()
    {
        return new BoxCollider2DComponent(Size, Offset, _density, _friction, _restitution, RestitutionThreshold, IsTrigger);
    }
}