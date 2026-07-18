# Input Handling

Handle keyboard and mouse input in game scripts (`ScriptableEntity`) or in batch logic systems (`IGameSystem` via `IKeyboardInput`).

## How input flows

1. **File**: `Engine/Platform/SilkNet/Input/SilkNetInputSystem.cs` — Silk.NET keyboard/mouse callbacks enqueue `InputEvent` records.
2. Each frame, `IInputSystem.Update` dequeues events and raises `InputReceived`.
3. **File**: `Engine/Core/Application.cs` — `HandleInputEvent` walks the layer stack top-down; a layer can set `event.IsHandled` to stop propagation.
4. In the standalone **Runtime** player, the active game layer updates `KeyboardInputState` and dispatches each `InputEvent` to every `ScriptableEntity` on the active scene.
5. In the **editor during Play mode**, `EditorInputHandler` applies the same keyboard state and script dispatch path before viewport tools run.

For global or shared input (turn order, menus), prefer `IKeyboardInput` in an `IGameSystem` — see [Scripting Tiers](scripting-tiers.md).

## Script callbacks

Override these methods in your `ScriptableEntity` subclass to respond to input:

| Method | When Called |
|--------|------------|
| `OnKeyPressed(KeyCodes key)` | A key is pressed down |
| `OnKeyReleased(KeyCodes keyCode)` | A key is released |
| `OnMouseButtonPressed(int button)` | A mouse button is pressed (0=left, 1=right, 2=middle) |
| `OnMouseButtonReleased(int button)` | A mouse button is released |
| `OnMouseMoved(float x, float y)` | The cursor moves (window coordinates) |
| `OnMouseScrolled(float xOffset, float yOffset)` | The scroll wheel moves |

## KeyCodes Reference

### Letters and Numbers

| Category | Keys |
|----------|------|
| Letters | `KeyCodes.A` through `KeyCodes.Z` |
| Numbers (top row) | `KeyCodes.D0` through `KeyCodes.D9` |
| Numpad | `KeyCodes.KeyPad0` through `KeyCodes.KeyPad9` |

### Navigation and Special Keys

| Category | Keys |
|----------|------|
| Arrows | `KeyCodes.Up`, `KeyCodes.Down`, `KeyCodes.Left`, `KeyCodes.Right` |
| Special | `KeyCodes.Space`, `KeyCodes.Enter`, `KeyCodes.Escape`, `KeyCodes.Tab`, `KeyCodes.Backspace`, `KeyCodes.Delete`, `KeyCodes.Insert` |
| Navigation | `KeyCodes.Home`, `KeyCodes.End`, `KeyCodes.PageUp`, `KeyCodes.PageDown` |
| Function | `KeyCodes.F1` through `KeyCodes.F25` |

### Modifier Keys

| Category | Keys |
|----------|------|
| Left modifiers | `KeyCodes.LeftShift`, `KeyCodes.LeftControl`, `KeyCodes.LeftAlt`, `KeyCodes.LeftSuper` |
| Right modifiers | `KeyCodes.RightShift`, `KeyCodes.RightControl`, `KeyCodes.RightAlt`, `KeyCodes.RightSuper` |

### Numpad Operations

`KeyCodes.KeyPadAdd`, `KeyCodes.KeyPadSubtract`, `KeyCodes.KeyPadMultiply`, `KeyCodes.KeyPadDivide`, `KeyCodes.KeyPadEnter`, `KeyCodes.KeyPadDecimal`, `KeyCodes.KeyPadEqual`

## Example: WASD Movement

A complete script for four-directional movement with velocity and damping:

```csharp
using System;
using System.Numerics;
using Input;
using SceneComponents;
using Scripting;

public class PlayerMovement : ScriptableEntity
{
    public float speed = 5.0f;
    private Vector3 _velocity = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        float dt = (float)ts.TotalSeconds;
        var transform = GetComponent<TransformComponent>();
        transform.Translation += _velocity * dt;
        _velocity *= 0.9f; // Apply damping
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        if (key == KeyCodes.W) _velocity.Y = speed;
        if (key == KeyCodes.S) _velocity.Y = -speed;
        if (key == KeyCodes.A) _velocity.X = -speed;
        if (key == KeyCodes.D) _velocity.X = speed;
    }

    public override void OnKeyReleased(KeyCodes keyCode)
    {
        if (keyCode == KeyCodes.W || keyCode == KeyCodes.S) _velocity.Y = 0;
        if (keyCode == KeyCodes.A || keyCode == KeyCodes.D) _velocity.X = 0;
    }
}
```

## Example: Jump on Space

```csharp
public override void OnKeyPressed(KeyCodes key)
{
    if (key == KeyCodes.Space)
    {
        var transform = GetComponent<TransformComponent>();
        transform.Translation += new Vector3(0, 2.0f, 0);
    }
}
```

## Example: Mouse Click

```csharp
public override void OnMouseButtonPressed(int button)
{
    if (button == 0) // Left click
    {
        Console.WriteLine("Left mouse button clicked!");
    }
    else if (button == 1) // Right click
    {
        Console.WriteLine("Right mouse button clicked!");
    }
}
```

## Example: Mouse scroll zoom

There are no `ZoomIn` / `ZoomOut` helpers on `ScriptableEntity`. Adjust the entity's transform or camera settings directly:

```csharp
public override void OnMouseScrolled(float xOffset, float yOffset)
{
    if (!HasComponent<CameraComponent>())
        return;

    var camera = GetComponent<CameraComponent>();
    camera.OrthographicSize -= yOffset * 0.5f;
}
```

## Polling in game systems

Inject `IKeyboardInput` into an `IGameSystem` to poll held or just-pressed keys without per-entity callbacks:

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Key is currently held |
| `WasKeyPressed(KeyCodes key)` | Key went down this frame (cleared when `EndFrame()` runs at end of update) |

**File**: `Engine/Core/Input/KeyboardInputState.cs` — singleton implementation registered in DI. `Application.HandleUpdate` calls `EndFrame()` each frame so `WasKeyPressed` only returns true for one frame.

## Common Patterns

**Velocity-based movement:** Accumulate velocity on key press, apply it in `OnUpdate`. This produces smoother movement and works well with physics simulation.

**Direct position manipulation:** Modify position directly in input callbacks. This is instant and snappy, but can conflict with physics if the entity has a `RigidBody2DComponent`.

## Next Steps

- [Physics](physics.md) -- collisions, triggers, and rigidbody interaction
- [API Reference](api-reference.md) -- complete method listing
- [Editor Keyboard Shortcuts](../editor/shortcuts.md) -- editor shortcut bindings and routing in Edit vs Play mode
