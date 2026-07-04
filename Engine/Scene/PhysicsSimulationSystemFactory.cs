using System.Numerics;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Scene.Systems;

namespace Engine.Scene;

internal sealed class PhysicsSimulationSystemFactory(PhysicsRuntimeBodyStore bodyStore, IContext context)
    : IPhysicsSimulationSystemFactory
{
    public ISystem Create()
    {
        var physicsWorld = new World(new Vector2(0, -9.8f));
        var contactListener = new SceneContactListener();
        physicsWorld.SetContactListener(contactListener);
        return new PhysicsSimulationSystem(physicsWorld, context, bodyStore);
    }
}
