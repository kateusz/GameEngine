using ECS;
using Engine.Physics;
using Engine.Scene.Systems;
using SceneComponents;
using Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneContactListener(PhysicsContactQueue contactQueue, ScriptRuntimeStore scriptStore) : IPhysicsContactListener
{
    private static readonly ILogger Logger = Log.ForContext<SceneContactListener>();

    public void OnContactBegin(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger)
    {
        try
        {
            var entityA = bodyA.Entity;
            var entityB = bodyB.Entity;
            if (entityA == null || entityB == null)
                return;

            if (isTrigger)
            {
                Logger.Debug("Trigger began between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                EnqueueAndNotifyTrigger(entityA, entityB, isBegin: true);
                EnqueueAndNotifyTrigger(entityB, entityA, isBegin: true);
            }
            else
            {
                Logger.Debug("Collision began between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                EnqueueAndNotifyCollision(entityA, entityB, isBegin: true);
                EnqueueAndNotifyCollision(entityB, entityA, isBegin: true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in OnContactBegin");
        }
    }

    public void OnContactEnd(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger)
    {
        try
        {
            var entityA = bodyA.Entity;
            var entityB = bodyB.Entity;
            if (entityA == null || entityB == null)
                return;

            if (isTrigger)
            {
                Logger.Debug("Trigger ended between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                EnqueueAndNotifyTrigger(entityA, entityB, isBegin: false);
                EnqueueAndNotifyTrigger(entityB, entityA, isBegin: false);
            }
            else
            {
                Logger.Debug("Collision ended between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                EnqueueAndNotifyCollision(entityA, entityB, isBegin: false);
                EnqueueAndNotifyCollision(entityB, entityA, isBegin: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in OnContactEnd");
        }
    }

    private void EnqueueAndNotifyTrigger(Entity entity, Entity otherEntity, bool isBegin)
    {
        contactQueue.Enqueue(new PhysicsContact(entity, otherEntity, IsTrigger: true, isBegin));
        NotifyEntityTrigger(entity, otherEntity, isBegin);
    }

    private void EnqueueAndNotifyCollision(Entity entity, Entity otherEntity, bool isBegin)
    {
        contactQueue.Enqueue(new PhysicsContact(entity, otherEntity, IsTrigger: false, isBegin));
        NotifyEntityCollision(entity, otherEntity, isBegin);
    }

    private void NotifyEntityTrigger(Entity entity, Entity otherEntity, bool isEnter)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        if (!scriptStore.TryGet(entity.Id, out var scriptableEntity))
            return;

        try
        {
            if (isEnter)
                scriptableEntity.OnTriggerEnter(otherEntity);
            else
                scriptableEntity.OnTriggerExit(otherEntity);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error calling trigger event on {EntityName}", entity.Name);
        }
    }

    private void NotifyEntityCollision(Entity entity, Entity otherEntity, bool isBegin)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        if (!scriptStore.TryGet(entity.Id, out var scriptableEntity))
            return;

        try
        {
            if (isBegin)
                scriptableEntity.OnCollisionBegin(otherEntity);
            else
                scriptableEntity.OnCollisionEnd(otherEntity);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error calling collision event on {EntityName}", entity.Name);
        }
    }
}
