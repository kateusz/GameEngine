using ECS;
using Engine.Physics;
using Engine.Scripting;
using SceneComponents;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneContactListener : IPhysicsContactListener
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
                NotifyEntityTrigger(entityA, entityB, true);
                NotifyEntityTrigger(entityB, entityA, true);
            }
            else
            {
                Logger.Debug("Collision began between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                NotifyEntityCollision(entityA, entityB, true);
                NotifyEntityCollision(entityB, entityA, true);
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
                NotifyEntityTrigger(entityA, entityB, false);
                NotifyEntityTrigger(entityB, entityA, false);
            }
            else
            {
                Logger.Debug("Collision ended between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                NotifyEntityCollision(entityA, entityB, false);
                NotifyEntityCollision(entityB, entityA, false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in OnContactEnd");
        }
    }

    private static void NotifyEntityTrigger(Entity entity, Entity otherEntity, bool isEnter)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        if (!ScriptRuntimeStore.TryGet(entity.Id, out var scriptableEntity))
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

    private static void NotifyEntityCollision(Entity entity, Entity otherEntity, bool isBegin)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        if (!ScriptRuntimeStore.TryGet(entity.Id, out var scriptableEntity))
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
