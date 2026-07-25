## Scripting and Games

### Scripting Tiers Separation
Data goes in IGameComponent+[SerializableComponent]; glue in ScriptableEntity+NativeScriptComponent; batch rules in IGameSystem+[Register]. Scripts are thin; systems own gameplay. Do not put tunable/shared state on script fields (they do not serialize).

**Sources:** documentation (confidence 92%)

```csharp
SnakeGameComponent = state; SnakeSystem = input + tick + visuals
Thin ScriptableEntity writes mailbox flags; IGameSystem consumes them
```

### Game Assembly Logic Stays In assets/scripts
Keep one-game logic in assets/scripts with [Register]; do not add Engine/ systems for a single game. Implement IComponent.Clone() on game components. Use project-relative asset paths, not absolute disk paths.

**Sources:** config, documentation (confidence 90%)

```csharp
New games/*.csproj should reference GameScriptSdk surface libraries, not Engine.csproj
```

### No ImGui In Published Games
Published games must not use ImGui or invent a UI framework; fake UI with sprites/quads (Snake SyncBanners pattern).

**Sources:** documentation (confidence 90%)

```csharp
Snake SyncBanners for score/banners via sprites
```
