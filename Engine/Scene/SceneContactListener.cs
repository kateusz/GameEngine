using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Dynamics.Contacts;
using Box2D.NetStandard.Dynamics.World;
using Box2D.NetStandard.Dynamics.World.Callbacks;
using ECS;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene;

public class SceneContactListener : ContactListener
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SceneContactListener>();
    
    public override void BeginContact(in Contact contact)
    {
        try
        {
            // Get the two fixtures that collided
            var fixtureA = contact.GetFixtureA();
            var fixtureB = contact.GetFixtureB();
            
            // Get the two bodies that collided
            var bodyA = fixtureA.GetBody();
            var bodyB = fixtureB.GetBody();
            
            // Get entities from bodies (we'll store entity reference in UserData)
            var entityA = bodyA.GetUserData<Entity>();
            var entityB = bodyB.GetUserData<Entity>();
            
            if (entityA == null || entityB == null)
            {
                // Bodies might not have entity references
                return;
            }
            
            // Check if either fixture is a sensor (trigger)
            bool isTrigger = fixtureA.IsSensor() || fixtureB.IsSensor();
            
            if (isTrigger)
            {
                Logger.Debug("Trigger began between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                // Notify trigger events
                NotifyEntityTrigger(entityA, entityB, true);
                NotifyEntityTrigger(entityB, entityA, true);
            }
            else
            {
                Logger.Debug("Collision began between {EntityA} and {EntityB}", entityA.Name, entityB.Name);
                // Notify collision events
                NotifyEntityCollision(entityA, entityB, true);
                NotifyEntityCollision(entityB, entityA, true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in BeginContact");
        }
    }
    
    public override void EndContact(in Contact contact)
    {
        try
        {
            var fixtureA = contact.GetFixtureA();
            var fixtureB = contact.GetFixtureB();
            var bodyA = fixtureA.GetBody();
            var bodyB = fixtureB.GetBody();
            
            var entityA = bodyA.GetUserData<Entity>();
            var entityB = bodyB.GetUserData<Entity>();
            
            if (entityA == null || entityB == null)
                return;
            
            bool isTrigger = fixtureA.IsSensor() || fixtureB.IsSensor();
            
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
            Logger.Error(ex, "Error in EndContact");
        }
    }

    public override void PreSolve(in Contact contact, in Manifold oldManifold)
    {
    }

    public override void PostSolve(in Contact contact, in ContactImpulse impulse)
    {
    }

    private void NotifyEntityTrigger(Entity entity, Entity otherEntity, bool isEnter)
    {
        // Check if entity has a script component
        if (!entity.HasComponent<NativeScriptComponent>())
            return;
            
        var scriptComponent = entity.GetComponent<NativeScriptComponent>();
        var scriptableEntity = scriptComponent.ScriptableEntity;
        
        if (scriptableEntity == null)
            return;
        
        try
        {
            if (isEnter)
            {
                scriptableEntity.OnTriggerEnter(otherEntity);
            }
            else
            {
                scriptableEntity.OnTriggerExit(otherEntity);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error calling trigger event on {EntityName}", entity.Name);
        }
    }
    
    private void NotifyEntityCollision(Entity entity, Entity otherEntity, bool isBegin)
    {
        // Check if entity has a script component
        if (!entity.HasComponent<NativeScriptComponent>())
            return;
            
        var scriptComponent = entity.GetComponent<NativeScriptComponent>();
        var scriptableEntity = scriptComponent.ScriptableEntity;
        
        if (scriptableEntity == null)
            return;
        
        try
        {
            if (isBegin)
            {
                scriptableEntity.OnCollisionBegin(otherEntity);
            }
            else
            {
                scriptableEntity.OnCollisionEnd(otherEntity);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error calling collision event on {EntityName}", entity.Name);
        }
    }
}