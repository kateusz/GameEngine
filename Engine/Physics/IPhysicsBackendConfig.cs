namespace Engine.Physics;

public interface IPhysicsBackendConfig
{
    PhysicsBackendType Type { get; }
    PhysicsBackendType Type3D { get; }
}
