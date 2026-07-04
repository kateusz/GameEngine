using System.Numerics;
using Engine.Platform.Box2D;

namespace Engine.Physics;

internal sealed class PhysicsWorld2DFactory(IPhysicsBackendConfig config) : IPhysicsWorld2DFactory
{
    public IPhysicsWorld2D Create(Vector2 gravity) =>
        config.Type switch
        {
            PhysicsBackendType.Box2D => new Box2DPhysicsWorld2D(gravity),
            _ => throw new NotSupportedException($"Unsupported physics backend: {config.Type}")
        };
}
