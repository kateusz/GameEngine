# Input Handling

Inject `IKeyboardInput` / `IMouseInput` into an `IGameSystem` — [Scripting Tiers](scripting-tiers.md).

## Flow

1. `SilkNetInputSystem` hooks the first Silk.NET keyboard and first mouse and enqueues `InputEvent` records from those callbacks
2. `IInputSystem.Update` dequeues and raises `InputReceived`
3. `KeyboardInputState.Apply` / `MouseInputState.Apply` update pollable device state
4. Systems poll that state on `OnUpdate`
5. `EndFrame()` clears per-frame edges. It is on the state classes, not on `IKeyboardInput` / `IMouseInput`

`SilkNetInputSystem` always constructs `KeyPressedEvent` with `IsRepeat = false`. Extra KeyDown callbacks while a key is held are queued as additional presses.

`Event.IsHandled` exists on the event base type; `Apply` does not read it.

## Polling in systems

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Held |
| `WasKeyPressed(KeyCodes key)` | Pressed this frame (cleared by `EndFrame`) |
| `IMouseInput.Position` | Window coordinates from `MouseMovedEvent` |
| `IMouseInput.Delta` | Last move this frame to the previous position; zero until the second move event |
| `IMouseInput.Scroll` | Wheel delta this frame (accumulated, then cleared by `EndFrame`) |
| `IsButtonDown(int button)` / `WasButtonPressed(int button)` | Platform button index (0 left, 1 right, 2 middle) |

A press and release in the same frame still reports `WasKeyPressed` / `WasButtonPressed` until `EndFrame`. `Delta` is the last move segment, not the sum of every move event that frame.

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

**Files:** `Engine/Core/Input/IInputSystem.cs`, `Engine/Core/Input/KeyboardInputState.cs`, `Engine/Core/Input/MouseInputState.cs`, `Engine/Platform/SilkNet/Input/SilkNetInputSystem.cs`
