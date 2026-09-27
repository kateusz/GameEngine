namespace SceneComponents.Physics;

/// <summary>
/// Shared physics material fields for 2D colliders.
/// </summary>
public interface ICollider2DMaterial
{
    float Density { get; set; }
    float Friction { get; set; }
    float Restitution { get; set; }
    bool IsTrigger { get; set; }
}
