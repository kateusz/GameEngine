## Systems

### Systems Own Logic And Priorities
One responsibility per system; no per-entity state in system fields; name with System suffix; inject deps via primary constructor; pick Priority from SystemPriorities ranges (physics ~100–130, game logic/scripts ~110–145, rendering ~150–180); register Priority in SystemPriorities.cs.

**Sources:** code-patterns, documentation (confidence 89%)

```csharp
public int Priority => SystemPriorities.PhysicsSimulationSystem;
internal sealed class SceneRenderSystem(...) : ISystem
```

### Unsubscribe In OnDetach
Mirror every OnAttach event subscription with an unsubscription in OnDetach to avoid leaks and callbacks on disposed scenes.

**Sources:** documentation (confidence 88%)
