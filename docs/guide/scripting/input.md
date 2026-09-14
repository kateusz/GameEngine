# Input Handling

Scripts: override `ScriptableEntity` callbacks. Systems: inject `IKeyboardInput` for shared/polled input — [Scripting Tiers](scripting-tiers.md).

## Flow

1. `SilkNetInputSystem` enqueues `InputEvent` records from Silk.NET callbacks
2. `IInputSystem.Update` dequeues and raises `InputReceived`
3. `Application` always applies the event to `KeyboardInputState` / `MouseInputState`
4. Layer stack (overlays first). A layer may set `event.IsHandled` to stop later layers.
5. **Runtime** and **editor Play**: remaining unhandled events go to `ScriptableEntity` callbacks (editor Play also skips mouse events outside `IPointerSurface`)

`Application` calls `KeyboardInputState.EndFrame()` / `MouseInputState.EndFrame()` after Draw. Those methods are not on the public poll interfaces.

## Script callbacks

| Method | When |
|--------|------|
| `OnKeyPressed(KeyCodes key)` | Key down |
| `OnKeyReleased(KeyCodes keyCode)` | Key up |
| `OnMouseButtonPressed(int button)` | Button down (0=left, 1=right, 2=middle) |
| `OnMouseButtonReleased(int button)` | Button up |
| `OnMouseMoved(float x, float y)` | Cursor move (window coords) |
| `OnMouseScrolled(float xOffset, float yOffset)` | Scroll wheel |

Keys are `KeyCodes.*` (letters, `D0`–`D9`, arrows, `Space`, `F1`–`F25`, modifiers, numpad).

## Example: velocity movement

```csharp
public override void OnUpdate(TimeSpan ts)
{
    var transform = GetComponent<TransformComponent>();
    transform.Translation += _velocity * (float)ts.TotalSeconds;
    _velocity *= 0.9f;
}

public override void OnKeyPressed(KeyCodes key)
{
    if (key == KeyCodes.W) _velocity.Y = speed;
    if (key == KeyCodes.S) _velocity.Y = -speed;
    if (key == KeyCodes.A) _velocity.X = -speed;
    if (key == KeyCodes.D) _velocity.X = speed;
}
```

Direct position changes in callbacks conflict with `RigidBody2DComponent` simulation — prefer velocity or physics forces.

## Polling in systems

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Held |
| `WasKeyPressed(KeyCodes key)` | Down this frame only |
| `IMouseInput.Position` | Window coords (same space as `IPointerSurface`) |
| `IMouseInput.Delta` | Movement this frame; zero until the second move event |
| `IMouseInput.Scroll` | Wheel delta this frame (accumulated) |
| `IsButtonDown` / `WasButtonPressed` | `MouseButtons.Left` (0), `Right` (1), `Middle` (2) |

**File:** `Engine/Core/Input/KeyboardInputState.cs`
