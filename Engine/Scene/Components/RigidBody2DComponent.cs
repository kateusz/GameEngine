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

public class RigidBody2DComponent : Component
{
    public RigidBodyType BodyType { get; set; }
    public bool FixedRotation { get; set; }
    
    [JsonIgnore]
    public Body RuntimeBody { get; set; }
}