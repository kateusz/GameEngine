using System.Numerics;
using Engine.Platform.Box2D;
using Engine.Platform.Bepu;

namespace Engine.Physics;

internal sealed class PhysicsWorld2DFactory(IPhysicsBackendConfig config) : IPhysicsWorld2DFactory
{
    public IPhysicsWorld2D Create(Vector2 gravity) =>
        config.Type == PhysicsBackendType.Box2D
            ? new Box2DPhysicsWorld2D(gravity)
            : throw new NotSupportedException($"Unsupported 2D physics backend: {config.Type}");

    public IPhysicsWorld3D Create3D(Vector3 gravity) => new BepuPhysicsWorld3D(gravity);
}
