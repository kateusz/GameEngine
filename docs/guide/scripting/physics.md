# Physics

2D physics via Box2D. Entities need `RigidBody2DComponent` + one 2D collider (`BoxCollider2DComponent`, `CircleCollider2DComponent`, or `EdgeCollider2DComponent`) to participate. Use one collider type per entity.

## Body types

| Type | Behavior |
|------|----------|
| **Static** | Immovable — walls, floors |
| **Dynamic** | Simulated — gravity, forces, collisions |
| **Kinematic** | Moved by code; pushes dynamics, ignores forces |

Set `RigidBody2DComponent.Velocity` in `OnUpdate` for Dynamic/Kinematic movement. `GravityScale` scales gravity per entity (1.0 = default).

## Collisions vs triggers

**Collision:** both entities need colliders; at least one needs a rigidbody.

**Trigger:** set the collider's `IsTrigger = true`. Overlap without physical response.

Poll `IPhysicsContacts.DrainContacts()` in an `IGameSystem` — [Scripting Tiers](scripting-tiers.md). Each `PhysicsContact` has `Self`, `Other`, `IsTrigger`, `IsBegin`.

## Collider shapes

| Component | Shape data |
|-----------|------------|
| `BoxCollider2DComponent` | `Size` (half-extents), `Offset` |
| `CircleCollider2DComponent` | `Radius`, `Offset` |
| `EdgeCollider2DComponent` | `Points` (open chain, ≥2) |

Shared material: `Density`, `Friction` (0–1), `Restitution` (bounciness 0–1). Box also serializes unused `RestitutionThreshold`.

## Example: pickup

Systems cannot destroy entities from typical samples — hide/remove components instead:

```csharp
foreach (var contact in contacts.DrainContacts())
{
    if (!contact.IsTrigger || !contact.IsBegin)
        continue;
    if (contact.Self.Name != "Coin" || contact.Other.Name != "Player")
        continue;
    if (contact.Self.HasComponent<SpriteRendererComponent>())
        contact.Self.RemoveComponent<SpriteRendererComponent>();
    if (contact.Self.HasComponent<BoxCollider2DComponent>())
        contact.Self.RemoveComponent<BoxCollider2DComponent>();
}
```

Setup: Static rigidbody, `IsTrigger` collider, sprite.

## Queries

Inject `IPhysicsQueries` — synchronous reads, no callbacks. Default hits solids only; pass `includeTriggers: true` for triggers.

```csharp
if (physics.Raycast(origin, new Vector2(0, -1), 0.6f, ignoreEntity: player) is { } ground)
    // standing on ground.Entity

if (physics.OverlapCircle(center, 2f, ignoreEntity: player) is { } nearby)
    // proximity
```
