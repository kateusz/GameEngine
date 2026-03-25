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

## Collision Callbacks

Override these methods to react when your entity physically collides with another:

```csharp
public override void OnCollisionBegin(Entity other)
{
    // Called when this entity collides with another
    if (other.HasComponent<TagComponent>())
    {
        var tag = other.GetComponent<TagComponent>();
        if (tag.Tag == "Enemy")
            Console.WriteLine("Hit an enemy!");
    }
}

public override void OnCollisionEnd(Entity other)
{
    // Called when the collision stops
}
```

Both entities must have rigidbodies and colliders for collision callbacks to fire.

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
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class Collectible : ScriptableEntity
{
    public override void OnTriggerEnter(Entity other)
    {
        if (other.HasComponent<TagComponent>() &&
            other.GetComponent<TagComponent>().Tag == "Player")
        {
            Console.WriteLine("Item collected!");
            DestroyEntity(Entity);
        }
    }
}
```

**Setup:** Create an entity with:
- `RigidBody2DComponent` (Static -- the pickup does not move)
- `BoxCollider2DComponent` (set `IsTrigger = true`)
- `SpriteRendererComponent` (so you can see it)
- `NativeScriptComponent` (set to "Collectible")

The player entity needs a `TagComponent` with `Tag = "Player"`.

## Example: Damage on Collision

```csharp
using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

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

Enable collider visualization in DebugSettings to see collider bounds in the viewport. This helps verify that your colliders are the right size and position.

## Next Steps

- [API Reference](api-reference.md) -- complete ScriptableEntity method listing
