using System.Numerics;
using System.Text.Json.Serialization;
using Box2D.NetStandard.Dynamics.Bodies;
using ECS;

namespace Engine.Scene.Components;

public enum RigidBodyType
{
    Static,
    Dynamic,
    Kinematic
}

public class RigidBody2DComponent : IComponent
{
    /// <summary>
    /// The type of physics body (Static, Dynamic, or Kinematic).
    /// Static: Immovable objects like walls and floors.
    /// Dynamic: Objects affected by forces and gravity.
    /// Kinematic: Movable but not affected by forces (controlled by animation or script).
    /// </summary>
    public RigidBodyType BodyType { get; set; }
    
    /// <summary>
    /// When true, prevents the body from rotating due to collisions or forces.
    /// Useful for characters that should remain upright or objects that shouldn't tumble.
    /// Default is false (rotation allowed).
    /// </summary>
    public bool FixedRotation { get; set; }

    [JsonIgnore]
    public Body RuntimeBody { get; set; }

    /// <summary>
    /// Previous position used for interpolation between physics steps.
    /// Stored before each physics step to enable smooth rendering.
    /// </summary>
    [JsonIgnore]
    public Vector2 PreviousPosition { get; set; }

    /// <summary>
    /// Previous rotation angle used for interpolation between physics steps.
    /// Stored before each physics step to enable smooth rendering.
    /// </summary>
    [JsonIgnore]
    public float PreviousAngle { get; set; }

    public IComponent Clone()
    {
        // Do not clone RuntimeBody as it's managed by the physics system
        return new RigidBody2DComponent
        {
            BodyType = BodyType,
            FixedRotation = FixedRotation,
            RuntimeBody = null
        };
    }
}