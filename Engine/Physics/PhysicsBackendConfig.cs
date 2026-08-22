namespace Engine.Physics;

internal sealed class PhysicsBackendConfig(PhysicsBackendType type) : IPhysicsBackendConfig
{
    public PhysicsBackendType Type { get; } = type;
}
