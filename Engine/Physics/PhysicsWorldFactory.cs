using System.Numerics;
using Engine.Platform.Box2D;

namespace Engine.Physics;

internal sealed class PhysicsWorldFactory(IPhysicsBackendConfig config) : IPhysicsWorldFactory
{
    public IPhysicsWorld2D Create(Vector2 gravity) =>
        config.Type == PhysicsBackendType.Box2D
            ? new Box2DPhysicsWorld2D(gravity)
            : throw new NotSupportedException($"Unsupported 2D physics backend: {config.Type}");
}
