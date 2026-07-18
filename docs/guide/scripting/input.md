# Input Handling

Scripts: override `ScriptableEntity` callbacks. Systems: inject `IKeyboardInput` for shared/polled input — [Scripting Tiers](scripting-tiers.md).

## Flow

1. `SilkNetInputSystem` enqueues `InputEvent` records from Silk.NET callbacks
2. `IInputSystem.Update` dequeues and raises `InputReceived`
3. `Application.HandleInputEvent` walks the layer stack; a layer can set `event.IsHandled` to stop propagation
4. **Runtime** and **editor Play**: keyboard state updates, then each `ScriptableEntity` on the active scene receives the event (editor: `EditorInputHandler` runs this before viewport tools)

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
| `WasKeyPressed(KeyCodes key)` | Down this frame only (`KeyboardInputState.EndFrame()` at end of update) |

**File:** `Engine/Core/Input/KeyboardInputState.cs`
