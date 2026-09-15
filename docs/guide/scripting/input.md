# Input Handling

Inject `IKeyboardInput` / `IMouseInput` into an `IGameSystem` — [Scripting Tiers](scripting-tiers.md).

## Flow

1. `SilkNetInputSystem` enqueues `InputEvent` records from Silk.NET callbacks
2. `IInputSystem.Update` dequeues and raises `InputReceived`
3. `Application` always applies the event to `KeyboardInputState` / `MouseInputState`
4. Layer stack (overlays first). A layer may set `event.IsHandled` to stop later layers.
5. **Runtime** and **editor Play**: systems poll device state on `OnUpdate`

`Application` calls `KeyboardInputState.EndFrame()` / `MouseInputState.EndFrame()` after Draw. Those methods are not on the public poll interfaces.

## Polling in systems

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Held |
| `WasKeyPressed(KeyCodes key)` | Down this frame only |
| `IMouseInput.Position` | Window coords (same space as `IPointerSurface`) |
| `IMouseInput.Delta` | Movement this frame; zero until the second move event |
| `IMouseInput.Scroll` | Wheel delta this frame (accumulated) |
| `IsButtonDown` / `WasButtonPressed` | `MouseButtons.Left` (0), `Right` (1), `Middle` (2) |

Keys are `KeyCodes.*` (letters, `D0`–`D9`, arrows, `Space`, `F1`–`F25`, modifiers, numpad).

## Example

```csharp
public void OnUpdate(TimeSpan deltaTime)
{
    var v = Vector2.Zero;
    if (keyboard.IsKeyDown(KeyCodes.W)) v.Y += 1f;
    if (keyboard.IsKeyDown(KeyCodes.S)) v.Y -= 1f;
    if (keyboard.IsKeyDown(KeyCodes.A)) v.X -= 1f;
    if (keyboard.IsKeyDown(KeyCodes.D)) v.X += 1f;
    // write velocity onto RigidBody2DComponent
}
```

Direct position changes conflict with `RigidBody2DComponent` simulation — prefer velocity.

**File:** `Engine/Core/Input/KeyboardInputState.cs`
