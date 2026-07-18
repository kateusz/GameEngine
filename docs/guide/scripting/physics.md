# Physics

2D physics via Box2D. Entities need `RigidBody2DComponent` + `BoxCollider2DComponent` to participate.

## Body types

| Type | Behavior |
|------|----------|
| **Static** | Immovable — walls, floors |
| **Dynamic** | Simulated — gravity, forces, collisions |
| **Kinematic** | Moved by code; pushes dynamics, ignores forces |

Set `RigidBody2DComponent.Velocity` in `OnUpdate` for Dynamic/Kinematic movement. `GravityScale` scales gravity per entity (1.0 = default).

## Collisions vs triggers

**Collision** (`OnCollisionBegin` / `OnCollisionEnd`): both entities need colliders; at least one needs a rigidbody.

**Trigger** (`OnTriggerEnter` / `OnTriggerExit`): set `BoxCollider2DComponent.IsTrigger = true`. Overlap without physical response.

Systems can poll `IPhysicsContacts.DrainContacts()` instead of script callbacks — [Scripting Tiers](scripting-tiers.md).

## Collider properties

`Density`, `Friction` (0–1), `Restitution` (bounciness 0–1), `RestitutionThreshold` (min speed to bounce).

## Example: pickup

Scripts cannot destroy entities — hide/remove components instead:

```csharp
public override void OnTriggerEnter(Entity other)
{
    if (_collected || other.Name != "Player") return;
    _collected = true;
    if (HasComponent<SpriteRendererComponent>()) RemoveComponent<SpriteRendererComponent>();
    if (HasComponent<BoxCollider2DComponent>()) RemoveComponent<BoxCollider2DComponent>();
}
```

Setup: Static rigidbody, `IsTrigger` collider, sprite, `NativeScriptComponent`.

## Queries

`Raycast` / `OverlapCircle` on `ScriptableEntity` — synchronous reads, no callbacks, auto-ignore self. Default hits solids only; pass `includeTriggers: true` for triggers.

```csharp
if (Raycast(Vector2.Zero, new Vector2(0, -1), 0.6f) is { } ground)
    // standing on ground.Entity

if (OverlapCircle(Vector2.Zero, 2f) is { } nearby)
    // proximity
```

Systems: inject `IPhysicsQueries` directly.

## Debug

**Show Collider Bounds** in debug settings (`PhysicsDebugRenderSystem`).
