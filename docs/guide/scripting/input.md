# Input Handling

Inject `IKeyboardInput` / `IMouseInput` into an `IGameSystem` — [Scripting Tiers](scripting-tiers.md).

## Flow

1. `SilkNetInputSystem` enqueues `InputEvent` records from Silk.NET callbacks
2. `IInputSystem.Update` dequeues and raises `InputReceived`
3. `Application.HandleInputEvent` walks the layer stack; a layer can set `event.IsHandled` to stop propagation
4. **Runtime** and **editor Play**: `KeyboardInputState` / `MouseInputState` update; systems poll them on `OnUpdate`

## Polling in systems

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Held |
| `WasKeyPressed(KeyCodes key)` | Down this frame only (`KeyboardInputState.EndFrame()` at end of update) |
| `IMouseInput.Position` | Window-space cursor |
| `IsButtonDown(MouseButtons button)` | Mouse button held |

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
