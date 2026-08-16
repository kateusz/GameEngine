using System.Numerics;
using Engine.Platform.Box2D;
using Engine.Platform.Bepu;

namespace Engine.Physics;

internal sealed class PhysicsWorldFactory(IPhysicsBackendConfig config) : IPhysicsWorldFactory
{
    public IPhysicsWorld2D Create(Vector2 gravity) =>
        config.Type == PhysicsBackendType.Box2D
            ? new Box2DPhysicsWorld2D(gravity)
            : throw new NotSupportedException($"Unsupported 2D physics backend: {config.Type}");

    public IPhysicsWorld3D Create3D(Vector3 gravity) =>
        config.Type3D == PhysicsBackendType.Bepu
            ? new BepuPhysicsWorld3D(gravity)
            : throw new NotSupportedException($"Unsupported 3D physics backend: {config.Type3D}");
}
