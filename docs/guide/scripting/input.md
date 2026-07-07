# Input Handling

Handle keyboard and mouse input in game scripts (`ScriptableEntity`) or in batch logic systems (`IGameSystem` via `IKeyboardInput`).

## How input flows

1. **File**: `Engine/Platform/SilkNet/Input/SilkNetInputSystem.cs` — Silk.NET keyboard/mouse callbacks enqueue `InputEvent` records.
2. Each frame, `IInputSystem.Update` dequeues events and raises `InputReceived`.
3. **File**: `Engine/Core/Application.cs` — `HandleInputEvent` walks the layer stack top-down; a layer can set `event.IsHandled` to stop propagation.
4. The active game layer updates `KeyboardInputState` and dispatches each `InputEvent` to every `ScriptableEntity` on the active scene.

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
| Arrows | `Up`, `Down`, `Left`, `Right` |
| Special | `Space`, `Enter`, `Escape`, `Tab`, `Backspace`, `Delete`, `Insert` |
| Navigation | `Home`, `End`, `PageUp`, `PageDown` |
| Function | `F1` through `F25` |

### Modifier Keys

| Category | Keys |
|----------|------|
| Left modifiers | `LeftShift`, `LeftControl`, `LeftAlt`, `LeftSuper` |
| Right modifiers | `RightShift`, `RightControl`, `RightAlt`, `RightSuper` |

### Numpad Operations

`KeyPadAdd`, `KeyPadSubtract`, `KeyPadMultiply`, `KeyPadDivide`, `KeyPadEnter`, `KeyPadDecimal`, `KeyPadEqual`

## Example: WASD Movement

A complete script for four-directional movement with velocity and damping:

```csharp
using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;
using Input;

public class PlayerMovement : ScriptableEntity
{
    public float speed = 5.0f;
    private Vector3 _velocity = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        float dt = (float)ts.TotalSeconds;
        var pos = GetPosition();
        SetPosition(pos + _velocity * dt);
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
        var pos = GetPosition();
        pos.Y += 2.0f;
        SetPosition(pos);
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

```csharp
public override void OnMouseScrolled(float xOffset, float yOffset)
{
    if (yOffset > 0) ZoomIn();
    else if (yOffset < 0) ZoomOut();
}
```

## Polling in game systems

Inject `IKeyboardInput` into an `IGameSystem` to poll held or just-pressed keys without per-entity callbacks:

| Method | Behavior |
|--------|----------|
| `IsKeyDown(KeyCodes key)` | Key is currently held |
| `WasKeyPressed(KeyCodes key)` | Key went down this frame (cleared at end of frame) |

**File**: `Engine/Core/Input/KeyboardInputState.cs` — singleton implementation registered in DI.

## Common Patterns

**Velocity-based movement:** Accumulate velocity on key press, apply it in `OnUpdate`. This produces smoother movement and works well with physics simulation.

**Direct position manipulation:** Modify position directly in input callbacks. This is instant and snappy, but can conflict with physics if the entity has a `RigidBody2DComponent`.

## Next Steps

- [Physics](physics.md) -- collisions, triggers, and rigidbody interaction
- [API Reference](api-reference.md) -- complete method listing
- [Editor Keyboard Shortcuts](../editor/shortcuts.md) -- editor shortcut bindings and routing in Edit vs Play mode
