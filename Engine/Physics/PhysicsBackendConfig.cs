namespace Engine.Physics;

internal sealed class PhysicsBackendConfig(
    PhysicsBackendType type,
    PhysicsBackendType type3D = PhysicsBackendType.Bepu) : IPhysicsBackendConfig
{
    public PhysicsBackendType Type { get; } = type;
    public PhysicsBackendType Type3D { get; } = type3D;
}
