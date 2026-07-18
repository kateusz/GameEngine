# Physics

Work with physics simulation, collisions, and triggers in your scripts.

## Setting Up Physics Entities

To make an entity participate in physics, add both of these components in the editor:

1. **RigidBody2DComponent** -- defines how the entity behaves in the physics world
2. **BoxCollider2DComponent** -- defines the collision shape

Both are required. A rigidbody without a collider has no shape to collide with. A collider without a rigidbody is not simulated.

## Body Types

| Type | Behavior | Use For |
|------|----------|---------|
| **Static** | Never moves. Other bodies bounce off it. | Walls, floors, platforms |
| **Dynamic** | Fully simulated by the physics engine (gravity, forces, collisions). | Players, enemies, projectiles |
| **Kinematic** | Moved by your code. Pushes dynamic bodies but is not affected by forces or gravity. | Moving platforms, elevators |

For **Dynamic** and **Kinematic** bodies, set `RigidBody2DComponent.Velocity` in `OnUpdate` to move via physics. The simulation copies velocity into the body before each step and writes the result back afterward. **Kinematic** bodies also follow transform changes you make in code before the step runs.

Use `GravityScale` on `RigidBody2DComponent` to scale gravity per entity (1.0 = default).

## Collision Callbacks

Override these methods to react when your entity physically collides with another:

```csharp
public override void OnCollisionBegin(Entity other)
{
    // Called when this entity collides with another
    if (other.Name == "Enemy")
        Console.WriteLine("Hit an enemy!");
}

public override void OnCollisionEnd(Entity other)
{
    // Called when the collision stops
}
```

Both entities need colliders for overlap detection. Collision callbacks fire when both sides have physics fixtures from `BoxCollider2DComponent` and at least one side has a `RigidBody2DComponent`.

## Trigger Callbacks

Triggers detect overlap without physical collision. Set `IsTrigger = true` on the `BoxCollider2DComponent`, then override these methods:

```csharp
public override void OnTriggerEnter(Entity other)
{
    // Entity entered the trigger zone
    Console.WriteLine("Something entered!");
}

public override void OnTriggerExit(Entity other)
{
    // Entity left the trigger zone
    Console.WriteLine("Something left!");
}
```

Triggers are useful for pickup zones, checkpoints, damage areas, and other regions that detect presence without blocking movement.

## Physics Properties Guide

These properties on `BoxCollider2DComponent` control how collisions feel:

| Property | What It Does | Range |
|----------|-------------|-------|
| **Density** | How heavy the object is for its size. Higher = more mass = harder to push. | 0+ |
| **Friction** | Surface grip. 0 = frictionless ice. 1 = sticky rubber. | 0 to 1 |
| **Restitution** | Bounciness. 0 = no bounce. 1 = perfectly elastic bounce. | 0 to 1 |
| **RestitutionThreshold** | Minimum collision speed for bounce to occur. Below this, the object stops instead of bouncing. | 0+ |

## Example: Collectible Pickup

A complete script for an item that gets collected when the player touches it:

```csharp
using System;
using ECS;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;

public class Collectible : ScriptableEntity
{
    private bool _collected;

    public override void OnTriggerEnter(Entity other)
    {
        if (_collected)
            return;

        if (other.Name == "Player")
        {
            Console.WriteLine("Item collected!");
            _collected = true;
            // Scripts cannot destroy entities today — hide the pickup instead.
            if (HasComponent<SpriteRendererComponent>())
                RemoveComponent<SpriteRendererComponent>();
            if (HasComponent<BoxCollider2DComponent>())
                RemoveComponent<BoxCollider2DComponent>();
        }
    }
}
```

**Setup:** Create an entity with:
- `RigidBody2DComponent` (Static -- the pickup does not move)
- `BoxCollider2DComponent` (set `IsTrigger = true`)
- `SpriteRendererComponent` (so you can see it)
- `NativeScriptComponent` (set to "Collectible")

Name the player entity **Player** in the Scene Hierarchy (or check a game component field instead).

## Example: Damage on Collision

```csharp
using System;
using ECS;
using Scripting;

public class DamageOnHit : ScriptableEntity
{
    public float damage = 10.0f;

    public override void OnCollisionBegin(Entity other)
    {
        Console.WriteLine($"Dealt {damage} damage to {other.Name}!");
    }
}
```

## Debug Tip

Enable **Show Collider Bounds** in debug settings to draw collider outlines in the play viewport (`PhysicsDebugRenderSystem`). This helps verify that your colliders are the right size and position.

Tier-2 `IGameSystem` scripts can poll `IPhysicsContacts.DrainContacts()` for batched contact events instead of using `ScriptableEntity` callbacks.

## Queries

Use raycasts and circle overlaps to probe the physics world from scripts. Queries are synchronous reads — they do not fire collision callbacks.

`Raycast` returns the closest hit along a ray. `OverlapCircle` returns one overlapping collider (order is unspecified when several overlap). Both helpers automatically ignore this entity's colliders.

```csharp
using System.Numerics;
using Scripting;

public override void OnUpdate(TimeSpan ts)
{
    // Ground check: cast down from the entity
    if (Raycast(new Vector2(0f, 0f), new Vector2(0f, -1f), 0.6f) is { } ground)
        Console.WriteLine($"Standing on {ground.Entity.Name}");

    // Proximity sensor
    if (OverlapCircle(new Vector2(0f, 0f), 2f) is { } nearby)
        Console.WriteLine($"Something nearby: {nearby.Entity.Name}");
}
```

By default, queries hit solid colliders only. Pass `includeTriggers: true` to detect trigger zones:

```csharp
if (Raycast(origin, direction, distance, includeTriggers: true) is { IsTrigger: true } hit)
    Console.WriteLine($"Hit trigger {hit.Entity.Name}");
```

Tier-2 `IGameSystem` scripts can call `IPhysicsQueries` directly from DI for world queries without going through `ScriptableEntity`.

## Next Steps

- [API Reference](api-reference.md) -- complete ScriptableEntity method listing
