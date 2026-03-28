# Scripting Getting Started

Get from zero to a working game script.

## What is a Script

A script is a C# class that extends `ScriptableEntity`. Scripts contain your custom game logic: movement, input handling, game rules, and anything else that makes your game unique. Scripts are compiled at runtime and support hot reload, so you can edit them without restarting the editor.

## Creating a Script

1. Select an entity in the Scene Hierarchy
2. In the Properties panel, click "Add Component" and choose `NativeScriptComponent`
3. Click "Create New Script" and enter a name
4. The engine generates a template file in your project's `assets/scripts/` folder

The generated template looks like this:

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class MyScript : ScriptableEntity
{
    public override void OnCreate()
    {
        Console.WriteLine("MyScript created!");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        // Your update logic here
    }

    public override void OnDestroy()
    {
        Console.WriteLine("MyScript destroyed!");
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        if (key == KeyCodes.Space)
        {
            Console.WriteLine("MyScript action triggered!");
        }
    }
}
```

## Attaching Scripts to Entities

Add a `NativeScriptComponent` to an entity via the Properties panel, then set `ScriptTypeName` to the name of your script class (e.g., "MyScript"). The engine finds and instantiates the script when play mode starts.

## Lifecycle Methods

Override these methods to hook into the engine's update loop:

| Method | When Called | Use For |
|--------|------------|---------|
| `OnCreate()` | Once when play mode starts | Initialization, finding other entities |
| `OnUpdate(TimeSpan ts)` | Every frame | Movement, game logic, state updates |
| `OnDestroy()` | When play mode stops | Cleanup |

The `ts` parameter in `OnUpdate` is the time elapsed since the last frame. Use `(float)ts.TotalSeconds` to get delta time in seconds.

## Hot Reload

Save your script file and the engine detects changes automatically. Scripts recompile without restarting the editor or stopping play mode. This lets you iterate quickly on gameplay logic.

## Exposed Fields

Public fields of supported types automatically appear in the Properties panel, letting you tweak values without editing code:

```csharp
public float speed = 5.0f;       // Slider in inspector
public bool isActive = true;     // Checkbox in inspector
public string label = "Hello";   // Text field in inspector
public Vector3 target;           // XYZ fields in inspector
```

**Supported types:** `int`, `float`, `double`, `bool`, `string`, `Vector2`, `Vector3`, `Vector4`

## Debugging

Use `Console.WriteLine()` to print messages. Output appears in the Console panel in the editor. This is the primary way to debug script behavior during development.

## Your First Script: Moving an Entity

Here is a complete script that moves an entity to the right:

```csharp
using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public class MoveRight : ScriptableEntity
{
    public float speed = 3.0f;

    public override void OnUpdate(TimeSpan ts)
    {
        float dt = (float)ts.TotalSeconds;
        var pos = GetPosition();
        pos.X += speed * dt;
        SetPosition(pos);
    }
}
```

To try it:

1. Create an entity and add a `SpriteRendererComponent` (so you can see it)
2. Add a `NativeScriptComponent` and set `ScriptTypeName` to "MoveRight"
3. Press Play -- the entity moves to the right
4. Change `speed` in the inspector to adjust speed without editing code

## Next Steps

- [Input Handling](input.md) -- respond to keyboard and mouse input
- [Physics](physics.md) -- collisions, triggers, and physics simulation
- [API Reference](api-reference.md) -- complete ScriptableEntity method reference
