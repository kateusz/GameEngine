using ECS;
using Engine.Physics;
using Engine.Scene.Systems;
using Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneContactListener(PhysicsContactQueue contactQueue) : IPhysicsContactListener
{
    private static readonly ILogger Logger = Log.ForContext<SceneContactListener>();

    public void OnContactBegin(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) =>
        EnqueuePair(bodyA.Entity, bodyB.Entity, isTrigger, isBegin: true);

    public void OnContactEnd(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) =>
        EnqueuePair(bodyA.Entity, bodyB.Entity, isTrigger, isBegin: false);

    private void EnqueuePair(Entity? entityA, Entity? entityB, bool isTrigger, bool isBegin)
    {
        try
        {
            if (entityA == null || entityB == null)
                return;

            contactQueue.Enqueue(new PhysicsContact(entityA, entityB, isTrigger, isBegin));
            contactQueue.Enqueue(new PhysicsContact(entityB, entityA, isTrigger, isBegin));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in contact {Phase}", isBegin ? "begin" : "end");
        }
    }
}
