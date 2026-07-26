## Components

### Components Are Data-Only
Components store data only; all game/per-frame logic belongs in systems. Matrix/transform helpers on components are allowed; game logic is not.

**Sources:** code-patterns, documentation, pr-reviews (confidence 100%)

```csharp
public class SpriteRendererComponent : IComponent { public IComponent Clone() => ...; }
public class TagComponent : IComponent { ... }
```

### Physics Pairing Requirement
Both `RigidBody2DComponent` and one 2D collider (`BoxCollider2DComponent`, `CircleCollider2DComponent`, or `EdgeCollider2DComponent`) are required on an entity for 2D physics simulation. Use a single collider type per entity (fixture priority if multiple are present: Box → Circle → Edge).

**Sources:** documentation (confidence 88%)
