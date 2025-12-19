# Getting Started

## Creating Your First Script

### Step 1: Create Script in Editor

1. Select an entity in the scene hierarchy
2. In the Properties panel, add a **NativeScriptComponent**
3. Click **"Create New Script"** button
4. Enter script name (e.g., `PlayerController`)
5. Click **"Create"**

This generates a script at `assets/scripts/PlayerController.cs` with the following template:

```csharp
using System;
using System.Numerics;
using GameEngine.Core;
using GameEngine.Scene;

namespace GameEngine.Scripts
{
    public class PlayerController : ScriptableEntity
    {
        public override void OnCreate()
        {
            // Called once when script is initialized
        }

        public override void OnUpdate(TimeSpan ts)
        {
            // Called every frame
            float deltaTime = (float)ts.TotalSeconds;
        }

        public override void OnDestroy()
        {
            // Called when entity is destroyed or scene stops
        }
    }
}
```

### Step 2: Add Script Logic

Modify the script to add behavior:

```csharp
public class PlayerController : ScriptableEntity
{
    private const float MoveSpeed = 5.0f;
    private Vector3 _velocity = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;

        // Move if we have velocity
        if (_velocity != Vector3.Zero && HasComponent<TransformComponent>())
        {
            var transform = GetComponent<TransformComponent>();
            transform.Translation += _velocity * MoveSpeed * deltaTime;
        }
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _velocity += Vector3.UnitY; break;
            case KeyCodes.S: _velocity -= Vector3.UnitY; break;
            case KeyCodes.A: _velocity -= Vector3.UnitX; break;
            case KeyCodes.D: _velocity += Vector3.UnitX; break;
        }
    }

    public override void OnKeyReleased(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _velocity -= Vector3.UnitY; break;
            case KeyCodes.S: _velocity += Vector3.UnitY; break;
            case KeyCodes.A: _velocity += Vector3.UnitX; break;
            case KeyCodes.D: _velocity -= Vector3.UnitX; break;
        }
    }
}
```

### Step 3: Hot Reload

The script **automatically recompiles** when you save the file. No need to restart the editor.

**Verification:**
- Check console for compilation messages
- Errors appear with file location and diagnostic info
- Script changes take effect immediately

## Directory Structure

```
YourProject/
└── assets/
    └── scripts/
        ├── PlayerController.cs
        ├── CameraController.cs
        └── EnemyAI.cs
```

**Location:** `assets/scripts/` relative to project root

**Requirements:**
- Scripts must inherit `ScriptableEntity`
- Must be in `GameEngine.Scripts` namespace (convention)
- File name should match class name

## Attaching Scripts to Entities

### Via Editor

1. Select entity
2. Add `NativeScriptComponent`
3. Use dropdown to select existing script
4. Click **"Attach"**

### Programmatically

```csharp
var entity = CreateEntity("Player");
var scriptComponent = entity.AddComponent<NativeScriptComponent>();
// Script engine instantiates script automatically on next frame
```

## Accessing Components

Scripts can access all ECS components on their entity:

```csharp
public override void OnUpdate(TimeSpan ts)
{
    // Check if component exists
    if (HasComponent<Sprite2DComponent>())
    {
        var sprite = GetComponent<Sprite2DComponent>();
        sprite.Color = new Vector4(1, 0, 0, 1); // Red tint
    }
}
```

## Next Steps

- [API Reference](./03-api-reference.md) - Complete API documentation
- [Examples](./07-examples.md) - Common patterns and code samples
- [Hot Reload](./04-hot-reload.md) - Understanding the reload system
