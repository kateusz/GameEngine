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
    public RigidBodyType BodyType { get; set; }
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