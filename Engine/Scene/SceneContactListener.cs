using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Dynamics.Contacts;
using Box2D.NetStandard.Dynamics.World;
using Box2D.NetStandard.Dynamics.World.Callbacks;
using ECS;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene;

/// <summary>
/// Represents a physics contact event for deferred processing.
/// Events are queued during World.Step() and processed afterward to ensure thread safety.
/// </summary>
public class PhysicsContactEvent
{
    public Entity EntityA { get; init; }
    public Entity EntityB { get; init; }
    public bool IsBegin { get; init; }
    public bool IsTrigger { get; init; }
}

/// <summary>
/// Listens for physics contact events from Box2D.
/// Queues events for deferred processing to maintain thread safety and enable future parallelization.
/// </summary>
public class SceneContactListener : ContactListener
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SceneContactListener>();
    
    private readonly Queue<PhysicsContactEvent> _contactQueue = new();
    
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
            
            // Queue event for deferred processing (thread-safe approach)
            _contactQueue.Enqueue(new PhysicsContactEvent
            {
                EntityA = entityA,
                EntityB = entityB,
                IsBegin = true,
                IsTrigger = isTrigger
            });
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
            
            // Queue event for deferred processing (thread-safe approach)
            _contactQueue.Enqueue(new PhysicsContactEvent
            {
                EntityA = entityA,
                EntityB = entityB,
                IsBegin = false,
                IsTrigger = isTrigger
            });
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

    /// <summary>
    /// Processes all queued contact events.
    /// Should be called after World.Step() completes to safely notify entities of collisions.
    /// This approach enables future parallelization of physics simulation.
    /// </summary>
    public void ProcessContactEvents()
    {
        while (_contactQueue.TryDequeue(out var evt))
        {
            if (evt.IsTrigger)
            {
                if (evt.IsBegin)
                {
                    Logger.Debug("Trigger began between {EntityA} and {EntityB}", evt.EntityA.Name, evt.EntityB.Name);
                }
                else
                {
                    Logger.Debug("Trigger ended between {EntityA} and {EntityB}", evt.EntityA.Name, evt.EntityB.Name);
                }
                
                NotifyEntityTrigger(evt.EntityA, evt.EntityB, evt.IsBegin);
                NotifyEntityTrigger(evt.EntityB, evt.EntityA, evt.IsBegin);
            }
            else
            {
                if (evt.IsBegin)
                {
                    Logger.Debug("Collision began between {EntityA} and {EntityB}", evt.EntityA.Name, evt.EntityB.Name);
                }
                else
                {
                    Logger.Debug("Collision ended between {EntityA} and {EntityB}", evt.EntityA.Name, evt.EntityB.Name);
                }
                
                NotifyEntityCollision(evt.EntityA, evt.EntityB, evt.IsBegin);
                NotifyEntityCollision(evt.EntityB, evt.EntityA, evt.IsBegin);
            }
        }
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