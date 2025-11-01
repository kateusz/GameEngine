# Animation System Usage Guide

**Version:** 1.0
**Last Updated:** 2025-10-31

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Editor Workflows](#editor-workflows)
4. [Component Reference](#component-reference)
5. [Animation Asset Format](#animation-asset-format)
6. [Scripting API](#scripting-api)
7. [Common Scenarios](#common-scenarios)
8. [Troubleshooting](#troubleshooting)
9. [Performance Tips](#performance-tips)

---

## Quick Reference

### Minimum Setup Requirements

To create an animated entity:

1. **Components Required:**T
   - `TransformComponent` (automatic)
   - `SubTextureRendererComponent` (manual)
   - `AnimationComponent` (manual)

2. **AnimationComponent Configuration:**
   - `AssetPath`: Path to `.anim` file (e.g., "Animations/player.anim")
   - `CurrentClipName`: Name of clip to play (auto-set on asset load)
   - `IsPlaying`: true (checkbox in editor)
   - `Loop`: true/false depending on animation type

3. **Files Required:**
   - `.anim` JSON file in Assets directory
   - Texture atlas (spritesheet PNG) referenced in `.anim`

### Most Common Issues

| Problem | Solution |
|---------|----------|
| Sprite not animating | Add `SubTextureRendererComponent` to entity |
| "Failed to load animation asset" | Check `AssetPath` is relative to Assets/ folder |
| Animation plays wrong clip | Set `CurrentClipName` or use `AnimationController.Play()` |
| Animation too fast/slow | Adjust `fps` in `.anim` JSON or `PlaybackSpeed` field |
| Events not firing | Check frame has `"events": ["eventName"]` in JSON |

### Editor Quick Actions

| Action | Steps |
|--------|-------|
| Load animation | Drag `.anim` from Content Browser onto "Browse..." button |
| Change clip | Use "Current Clip" dropdown |
| Preview frame-by-frame | Uncheck "Playing", drag "Frame" scrubber |
| Adjust speed | Drag "Speed" slider (0.1x - 3.0x) |
| Toggle looping | Check/uncheck "Loop" checkbox |

---

## Overview

The Animation System provides frame-based 2D sprite animation using texture atlases (spritesheets). It integrates seamlessly with the ECS architecture and supports:

- ✅ Multiple animation clips per asset
- ✅ Frame events for gameplay integration
- ✅ Playback control (play, pause, stop, speed adjustment)
- ✅ Frame scrubbing in editor
- ✅ Looping and one-shot animations
- ✅ Asset caching with reference counting
- ✅ Hot-reloadable animation assets

**Key Components:**
- `AnimationComponent` - Data component attached to animated entities
- `AnimationSystem` - System that updates all animations each frame
- `AnimationController` - Static API for controlling animations from scripts
- `AnimationAssetManager` - Manages loading/unloading of animation assets

---

## Quick Start

### 1. Create an Animation Asset

Create a JSON file in your assets directory (e.g., `Assets/Animations/player.anim`):

```json
{
  "id": "player.animations",
  "atlas": "Textures/Characters/player_spritesheet.png",
  "cellSize": [32, 32],
  "origin": "bottom-left",
  "animations": {
    "idle": {
      "fps": 8,
      "loop": true,
      "frames": [
        {
          "rect": [0, 0, 32, 32],
          "pivot": [0.5, 0.0],
          "flip": [false, false],
          "rotation": 0.0,
          "scale": [1.0, 1.0],
          "events": []
        },
        {
          "rect": [32, 0, 32, 32],
          "pivot": [0.5, 0.0],
          "flip": [false, false],
          "rotation": 0.0,
          "scale": [1.0, 1.0],
          "events": []
        }
      ]
    },
    "walk": {
      "fps": 12,
      "loop": true,
      "frames": [
        {
          "rect": [0, 32, 32, 32],
          "pivot": [0.5, 0.0],
          "events": ["footstep"]
        },
        {
          "rect": [32, 32, 32, 32],
          "pivot": [0.5, 0.0],
          "events": []
        }
      ]
    }
  }
}
```

### 2. Add Components in Editor

1. Select an entity in the Scene Hierarchy
2. Add **AnimationComponent**:
   - Click **"Add Component"** button
   - Select **"Animation"** from the component list
3. Add **SubTextureRendererComponent** (required for rendering):
   - Click **"Add Component"** button
   - Select **"SubTextureRenderer"** from the component list
4. Configure **AnimationComponent**:
   - Drag and drop your `.anim` file from Content Browser into the **"Browse..."** button area
   - The first animation clip will start playing automatically
   - Default settings:
     - `IsPlaying`: true (checkbox "Playing")
     - `Loop`: true (checkbox "Loop")
     - `PlaybackSpeed`: 1.0 (slider "Speed")
     - `CurrentClipName`: first clip in the asset
     - `ShowDebugInfo`: false (checkbox "Show Debug")

### 3. Programmatic Setup (Optional)

If you need to set up animation components via code:

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;
using Engine.Scene.Components;

public class AnimatedEntitySpawner : ScriptableEntity
{
    protected override void OnInit()
    {
        // Create or get entity
        var entity = new Entity();

        // Add required components (TransformComponent is already added automatically)
        var subTextureRenderer = new SubTextureRendererComponent();
        entity.AddComponent(subTextureRenderer);

        var animComponent = new AnimationComponent
        {
            AssetPath = "Animations/Characters/player.anim",  // Required: path to .anim file
            IsPlaying = true,                                 // Start playing immediately
            Loop = true,                                      // Loop the animation
            PlaybackSpeed = 1.0f,                            // Normal speed
            ShowDebugInfo = false                            // No debug overlay
            // CurrentClipName will be set automatically when asset loads
        };
        entity.AddComponent(animComponent);

        // Start playing specific clip (do this after adding component)
        AnimationController.Play(entity, "idle");
    }
}
```

### 4. Control from Script

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;

public class PlayerController : ScriptableEntity
{
    protected override void OnUpdate(TimeSpan deltaTime)
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");

        // Switch animations based on movement
        if (Math.Abs(horizontal) > 0.1f)
        {
            AnimationController.Play(Entity, "walk");
        }
        else
        {
            AnimationController.Play(Entity, "idle");
        }
    }
}
```

**Important Notes:**
- `AnimationController.Play()` is safe to call every frame - it only switches if the clip name is different
- Always add `SubTextureRendererComponent` before `AnimationComponent`
- The `Asset` field is automatically populated by `AnimationSystem` when `AssetPath` is set

---

## Editor Workflows

### Scenario 1: Setting Up a Basic Animated Sprite

**Goal:** Create a character with idle and walk animations.

**Steps:**

1. **Create Entity**
   - Right-click in Scene Hierarchy → **"Create Empty Entity"**
   - Rename to "Player"

2. **Add Required Components**
   - **`TransformComponent`** (automatically added with every entity)
   - Add **`SubTextureRendererComponent`**:
     - Click **"Add Component"** → Select "SubTextureRenderer"
     - No configuration needed (automatically managed by AnimationSystem)
   - Add **`AnimationComponent`**:
     - Click **"Add Component"** → Select "Animation"

3. **Configure AnimationComponent**
   - **Asset Path** (required):
     - Drag `.anim` file from Content Browser onto the **"Browse..."** button
     - OR manually set `AssetPath` property to path like "Animations/Characters/player.anim"
   - **Current Clip** (automatically set):
     - First clip in asset is selected by default
     - Change via dropdown: **"Current Clip"** → Select clip name
   - **Playback Settings**:
     - **Playing** checkbox: Check to start animation (default: checked after drop)
     - **Loop** checkbox: Check to repeat animation (default: checked)
     - **Speed** slider: Adjust speed 0.1x to 3.0x (default: 1.0x)

4. **Verify Animation in Viewport**
   - Animated sprite should appear and play automatically
   - Use **Timeline** scrubber to manually preview frames

---

### Scenario 2: Previewing Animation Clips

**Goal:** Preview different animation clips without modifying the scene.

**Steps:**

1. **Select Entity** with AnimationComponent

2. **In Properties Panel → Available Clips Section:**
   - Each clip shows: `clipName (frameCount frames, fps fps)`
   - Click **"Preview"** button next to any clip

3. **What Happens:**
   - Selected clip plays once (loop disabled)
   - Animation stops on last frame
   - You can switch back to original clip manually

**Tip:** Use this to test animations before setting them up in scripts.

---

### Scenario 3: Frame-by-Frame Animation Editing

**Goal:** Fine-tune animation timing by manually scrubbing frames.

**Steps:**

1. **Select Entity** with AnimationComponent

2. **Pause Animation:**
   - Click **"⏸"** button in Timeline section
   - OR uncheck **"Playing"** checkbox

3. **Scrub Frames:**
   - Drag the **"Frame"** slider left/right
   - Each frame updates in viewport immediately
   - Frame info shows current frame data:
     - Rect: `[x, y, width, height]`
     - Pivot: `[x, y]`
     - Events: List of event names for this frame

4. **Inspect Frame Details:**
   - Look at **"Frame Info"** section
   - Check if frame has events defined
   - Verify texture coordinates are correct

**Use Case:** Finding exact frame for hitbox activation or sound effect triggers.

---

### Scenario 4: Adjusting Animation Speed

**Goal:** Make animation play faster or slower without modifying asset.

**Steps:**

1. **Select Entity** with AnimationComponent

2. **In Playback Section:**
   - Drag **"Speed"** slider
   - Range: 0.1x (very slow) to 3.0x (very fast)
   - Default: 1.0x (normal speed)

3. **Live Preview:**
   - Animation speed changes immediately in viewport
   - Timeline shows current time: `Time: 0.25s / 0.67s`

**Use Cases:**
- Slow-motion effects (0.5x)
- Fast-forward for idle animations (1.5x)
- Debug timing issues (0.1x)

---

### Scenario 5: Switching Animation Clips at Runtime

**Goal:** Test different animation transitions in editor.

**Steps:**

1. **Enter Play Mode** (click play button in toolbar)

2. **While Playing:**
   - Select animated entity
   - Change **"Current Clip"** dropdown
   - New animation starts immediately

3. **Observe Transition:**
   - No crossfade (instant switch)
   - Frame resets to 0 of new clip
   - Loop setting maintained

**Note:** In play mode, changes are temporary (reset when stopping).

---

## Component Reference

### AnimationComponent Fields

Complete reference for all AnimationComponent properties:

| Field | Type | Default | Editor UI | Description |
|-------|------|---------|-----------|-------------|
| `AssetPath` | string? | null | Drag-drop area | Path to `.anim` file (relative to Assets/) |
| `Asset` | AnimationAsset? | null | (read-only) | Loaded asset reference (managed by system) |
| `CurrentClipName` | string | "" | "Current Clip" dropdown | Name of currently playing animation clip |
| `IsPlaying` | bool | false | "Playing" checkbox | Whether animation is actively playing |
| `Loop` | bool | true | "Loop" checkbox | Whether to loop when reaching last frame |
| `PlaybackSpeed` | float | 1.0f | "Speed" slider (0.1-3.0) | Playback speed multiplier |
| `ShowDebugInfo` | bool | false | "Show Debug" checkbox | Display debug overlay in viewport |
| `CurrentFrameIndex` | int | 0 | "Frame" scrubber | Current frame index (0-based, runtime only) |
| `FrameTimer` | float | 0.0f | (internal) | Frame timing accumulator (runtime only) |
| `PreviousFrameIndex` | int | -1 | (internal) | Previous frame for event detection (runtime only) |

**Notes:**
- `AssetPath` is **required** - animation won't play without it
- `Asset` is automatically loaded by AnimationSystem when `AssetPath` is set
- Runtime fields (`CurrentFrameIndex`, `FrameTimer`, `PreviousFrameIndex`) are managed by AnimationSystem
- Changes to `IsPlaying`, `Loop`, and `PlaybackSpeed` take effect immediately

### SubTextureRendererComponent Fields

This component is **required** for AnimationComponent to display sprites:

| Field | Type | Default | Editor UI | Description |
|-------|------|---------|-----------|-------------|
| `Texture` | Texture2D? | null | (managed by system) | Current texture (set by AnimationSystem) |
| `Coords` | Vector2 | (0, 0) | (managed by system) | Current UV coordinates (set by AnimationSystem) |
| `CellSize` | Vector2 | (16, 16) | Not exposed | Size of each cell in pixels (set by AnimationSystem) |
| `SpriteSize` | Vector2 | (1, 1) | Not exposed | Size in cells (set by AnimationSystem) |

**Notes:**
- AnimationSystem **automatically updates** all fields each frame
- Do **not** manually modify these fields when using AnimationComponent
- If entity has both AnimationComponent and SubTextureRendererComponent, animation takes full control

### Component Setup Checklist

For a fully functional animated entity, ensure:

- ✅ **TransformComponent** exists (position, rotation, scale)
- ✅ **SubTextureRendererComponent** exists (rendering)
- ✅ **AnimationComponent** exists with valid `AssetPath`
- ✅ Animation asset file (`.anim`) exists at specified path
- ✅ Texture atlas (`.png`) referenced in `.anim` exists
- ✅ At least one animation clip defined in `.anim` file

---

## Animation Asset Format

### JSON Structure

```json
{
  "id": "unique-asset-id",
  "atlas": "path/to/spritesheet.png",
  "cellSize": [width, height],
  "origin": "bottom-left",
  "animations": {
    "clipName": {
      "fps": 12.0,
      "loop": true,
      "frames": [ /* frame array */ ]
    }
  }
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique identifier for this asset |
| `atlas` | string | Yes | Path to spritesheet texture (relative to Assets/) |
| `cellSize` | [int, int] | Yes | Default cell size [width, height] in pixels |
| `origin` | string | Yes | Coordinate system: "bottom-left" (OpenGL standard) |
| `animations` | object | Yes | Dictionary of clip name → clip definition |

### Clip Definition

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `fps` | float | Yes | - | Frames per second (e.g., 12.0) |
| `loop` | bool | No | false | Whether animation loops after last frame |
| `frames` | array | Yes | - | Array of frame definitions (min 1 frame) |

### Frame Definition

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `rect` | [int, int, int, int] | Yes | - | Frame rectangle [x, y, width, height] in pixels |
| `pivot` | [float, float] | No | [0.5, 0.0] | Normalized pivot [x, y] in range 0..1 |
| `flip` | [bool, bool] | No | [false, false] | Horizontal and vertical flip [flipH, flipV] |
| `rotation` | float | No | 0.0 | Rotation in degrees (not yet implemented) |
| `scale` | [float, float] | No | [1.0, 1.0] | Per-frame scale (not yet implemented) |
| `events` | array[string] | No | [] | Event names to fire when entering this frame |

### Coordinate System

- **Origin:** Bottom-left (OpenGL standard)
- **X-axis:** 0 (left) → texture.width (right)
- **Y-axis:** 0 (bottom) → texture.height (top)

**Example Rect Calculation:**

```
Spritesheet: 128x64 pixels
Cell Size: 32x32
Frame at grid position (2, 1):
rect = [2 * 32, 1 * 32, 32, 32] = [64, 32, 32, 32]
```

---

## Scripting API

### AnimationController Static Methods

All animation control is done through the `AnimationController` static class:

```csharp
using Engine.Animation;
```

### Basic Controls

#### Play Animation

```csharp
// Play animation clip by name
AnimationController.Play(Entity entity, string clipName);

// Force restart even if already playing same clip
AnimationController.Play(Entity entity, string clipName, forceRestart: true);
```

**Example:**
```csharp
// Switch to walk animation
AnimationController.Play(Entity, "walk");

// Restart current animation from frame 0
AnimationController.Play(Entity, "idle", forceRestart: true);
```

---

#### Stop Animation

```csharp
// Stop and reset to frame 0
AnimationController.Stop(Entity entity);
```

**Example:**
```csharp
protected override void OnDestroy()
{
    AnimationController.Stop(Entity);
}
```

---

#### Pause/Resume Animation

```csharp
// Pause without resetting
AnimationController.Pause(Entity entity);

// Resume from paused state
AnimationController.Resume(Entity entity);
```

**Example:**
```csharp
if (isPaused)
    AnimationController.Pause(Entity);
else
    AnimationController.Resume(Entity);
```

---

### Advanced Controls

#### Set Playback Speed

```csharp
AnimationController.SetSpeed(Entity entity, float speed);
```

**Example:**
```csharp
// Slow motion effect
AnimationController.SetSpeed(Entity, 0.5f);

// Normal speed
AnimationController.SetSpeed(Entity, 1.0f);

// Fast forward
AnimationController.SetSpeed(Entity, 2.0f);
```

---

#### Jump to Specific Frame

```csharp
// Jump to frame index (0-based)
AnimationController.SetFrame(Entity entity, int frameIndex);

// Jump to normalized time (0..1 range)
AnimationController.SetNormalizedTime(Entity entity, float t);
```

**Example:**
```csharp
// Jump to frame 5
AnimationController.SetFrame(Entity, 5);

// Jump to middle of animation
AnimationController.SetNormalizedTime(Entity, 0.5f);

// Jump to end
AnimationController.SetNormalizedTime(Entity, 1.0f);
```

---

### State Queries

#### Get Current State

```csharp
int currentFrame = AnimationController.GetCurrentFrame(Entity entity);
int totalFrames = AnimationController.GetFrameCount(Entity entity);
float progress = AnimationController.GetNormalizedTime(Entity entity);
string clipName = AnimationController.GetCurrentClipName(Entity entity);
bool isPlaying = AnimationController.IsPlaying(Entity entity);
```

**Example:**
```csharp
// Check if attack animation finished
if (AnimationController.GetCurrentClipName(Entity) == "attack" &&
    !AnimationController.IsPlaying(Entity))
{
    // Attack completed, switch to idle
    AnimationController.Play(Entity, "idle");
}
```

---

#### Clip Information

```csharp
// Get all available clips
string[] clips = AnimationController.GetAvailableClips(Entity entity);

// Check if clip exists
bool hasClip = AnimationController.HasClip(Entity entity, string clipName);

// Get clip duration in seconds
float duration = AnimationController.GetClipDuration(Entity entity, string clipName);
```

**Example:**
```csharp
// Print all available animations
foreach (string clip in AnimationController.GetAvailableClips(Entity))
{
    float duration = AnimationController.GetClipDuration(Entity, clip);
    Debug.Log($"Clip: {clip}, Duration: {duration:F2}s");
}
```

---

## Common Scenarios

### Scenario 1: Simple Character State Machine

**Goal:** Switch between idle, walk, and jump animations based on input.

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;

public class PlayerController : ScriptableEntity
{
    private bool _isGrounded = true;

    protected override void OnUpdate(TimeSpan deltaTime)
    {
        float horizontal = Input.GetAxis("Horizontal");
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

        // Determine animation based on state
        if (!_isGrounded)
        {
            AnimationController.Play(Entity, "jump");
        }
        else if (Math.Abs(horizontal) > 0.1f)
        {
            AnimationController.Play(Entity, "walk");
        }
        else
        {
            AnimationController.Play(Entity, "idle");
        }
    }
}
```

**Note:** `Play()` only switches if clip is different, so calling every frame is safe.

---

### Scenario 2: Attack Animation with Callback

**Goal:** Play attack animation and detect when it finishes.

**Animation JSON:**
```json
{
  "animations": {
    "attack": {
      "fps": 15,
      "loop": false,
      "frames": [
        { "rect": [0, 64, 32, 32], "events": ["attack_start"] },
        { "rect": [32, 64, 32, 32], "events": ["attack_hit"] },
        { "rect": [64, 64, 32, 32], "events": ["attack_end"] }
      ]
    }
  }
}
```

**Script:**
```csharp
using ECS;
using Engine.Animation;
using Engine.Events;
using Engine.Scene;

public class PlayerCombat : ScriptableEntity
{
    private bool _isAttacking = false;

    protected override void OnInit()
    {
        // Subscribe to animation events
        EventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrameEvent);
        EventBus.Subscribe<AnimationCompleteEvent>(OnAnimationComplete);
    }

    protected override void OnUpdate(TimeSpan deltaTime)
    {
        if (Input.GetKeyDown(KeyCode.J) && !_isAttacking)
        {
            Attack();
        }
    }

    private void Attack()
    {
        _isAttacking = true;
        AnimationController.Play(Entity, "attack", forceRestart: true);
    }

    private void OnAnimationFrameEvent(AnimationFrameEvent evt)
    {
        // Filter by entity
        if (evt.Entity != Entity) return;

        // Handle attack events
        switch (evt.EventName)
        {
            case "attack_start":
                Logger.Info("Attack started");
                break;

            case "attack_hit":
                DealDamageToEnemies();
                break;

            case "attack_end":
                Logger.Info("Attack ended");
                break;
        }
    }

    private void OnAnimationComplete(AnimationCompleteEvent evt)
    {
        // Filter by entity
        if (evt.Entity != Entity) return;

        // Attack animation finished
        if (evt.ClipName == "attack")
        {
            _isAttacking = false;
            AnimationController.Play(Entity, "idle");
        }
    }

    private void DealDamageToEnemies()
    {
        // TODO: Implement damage logic
        Logger.Info("Dealing damage!");
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<AnimationFrameEvent>(OnAnimationFrameEvent);
        EventBus.Unsubscribe<AnimationCompleteEvent>(OnAnimationComplete);
    }
}
```

---

### Scenario 3: Footstep Sound Effects

**Goal:** Play sound when "footstep" event fires during walk animation.

**Animation JSON:**
```json
{
  "animations": {
    "walk": {
      "fps": 12,
      "loop": true,
      "frames": [
        { "rect": [0, 32, 32, 32], "events": ["footstep"] },
        { "rect": [32, 32, 32, 32], "events": [] },
        { "rect": [64, 32, 32, 32], "events": [] },
        { "rect": [96, 32, 32, 32], "events": ["footstep"] }
      ]
    }
  }
}
```

**Script:**
```csharp
using ECS;
using Engine.Animation;
using Engine.Audio;
using Engine.Events;
using Engine.Scene;

public class FootstepSounds : ScriptableEntity
{
    private AudioClip? _footstepSound;

    protected override void OnInit()
    {
        // Load footstep sound
        _footstepSound = AudioClip.Load("Audio/footstep.wav");

        // Subscribe to frame events
        EventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrameEvent);
    }

    private void OnAnimationFrameEvent(AnimationFrameEvent evt)
    {
        // Filter by entity and event name
        if (evt.Entity != Entity) return;
        if (evt.EventName != "footstep") return;

        // Play footstep sound
        if (_footstepSound != null)
        {
            AudioSystem.Play(Entity, _footstepSound);
        }
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<AnimationFrameEvent>(OnAnimationFrameEvent);
    }
}
```

---

### Scenario 4: Direction-Based Animation

**Goal:** Flip sprite based on movement direction.

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;
using Engine.Scene.Components;

public class DirectionalSprite : ScriptableEntity
{
    private float _lastDirection = 1.0f; // 1 = right, -1 = left

    protected override void OnUpdate(TimeSpan deltaTime)
    {
        float horizontal = Input.GetAxis("Horizontal");

        // Update direction
        if (Math.Abs(horizontal) > 0.1f)
        {
            _lastDirection = Math.Sign(horizontal);

            // Flip sprite
            var transform = Entity.GetComponent<TransformComponent>();
            transform.Scale = new Vector3(
                Math.Abs(transform.Scale.X) * _lastDirection,
                transform.Scale.Y,
                transform.Scale.Z
            );

            AnimationController.Play(Entity, "walk");
        }
        else
        {
            AnimationController.Play(Entity, "idle");
        }
    }
}
```

**Alternative:** Use flip flags in animation JSON (per-frame flipping).

---

### Scenario 5: Health-Based Animation Speed

**Goal:** Slow down animation when health is low.

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;

public class HealthAnimation : ScriptableEntity
{
    private float _currentHealth = 100f;
    private float _maxHealth = 100f;

    protected override void OnUpdate(TimeSpan deltaTime)
    {
        // Calculate speed based on health percentage
        float healthPercent = _currentHealth / _maxHealth;
        float animSpeed = Mathf.Lerp(0.5f, 1.0f, healthPercent);

        AnimationController.SetSpeed(Entity, animSpeed);
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth < 0) _currentHealth = 0;
    }
}
```

---

## Troubleshooting

### Issue 1: Animation Not Playing

**Symptoms:** Entity has AnimationComponent but sprite doesn't animate.

**Diagnostic Checklist:**

1. **Missing SubTextureRendererComponent (MOST COMMON)**
   ```csharp
   // Check in editor: Does entity have SubTextureRendererComponent?
   // Add manually: "Add Component" → "SubTextureRenderer"

   // Or add via script:
   if (!entity.HasComponent<SubTextureRendererComponent>())
       entity.AddComponent<SubTextureRendererComponent>();
   ```

2. **AssetPath not set**
   ```csharp
   var anim = entity.GetComponent<AnimationComponent>();
   if (string.IsNullOrEmpty(anim.AssetPath))
       Logger.Error("AssetPath is empty! Drag .anim file in editor.");
   ```

3. **Asset failed to load**
   ```csharp
   if (anim.Asset == null && !string.IsNullOrEmpty(anim.AssetPath))
       Logger.Error($"Animation asset failed to load: {anim.AssetPath}");
   ```

4. **IsPlaying is false**
   ```csharp
   // Check editor: Is "Playing" checkbox checked?
   if (!anim.IsPlaying)
       AnimationController.Play(entity, "idle");
   ```

5. **CurrentClipName is empty**
   ```csharp
   if (string.IsNullOrEmpty(anim.CurrentClipName))
       Logger.Error("No clip selected! Use AnimationController.Play()");
   ```

6. **Check console for errors:**
   - Look for "Failed to load animation asset" messages
   - Verify `.anim` file path is correct (relative to Assets/)
   - Verify texture atlas path in `.anim` JSON is correct

---

### Issue 2: Wrong Frame Rate

**Symptoms:** Animation plays too fast or too slow.

**Checks:**

1. **Verify FPS in JSON:**
   ```json
   "fps": 12.0  // ← Check this value
   ```

2. **Check playback speed:**
   ```csharp
   var anim = entity.GetComponent<AnimationComponent>();
   Logger.Info($"Playback speed: {anim.PlaybackSpeed}x");
   // Reset to normal:
   AnimationController.SetSpeed(entity, 1.0f);
   ```

3. **Frame duration calculation:**
   - Frame duration = 1.0 / FPS
   - 12 FPS = 0.083s per frame
   - 8 FPS = 0.125s per frame

---

### Issue 3: Events Not Firing

**Symptoms:** AnimationFrameEvent not received in script.

**Checks:**

1. **Are events defined in JSON?**
   ```json
   "frames": [
     { "rect": [0, 0, 32, 32], "events": ["footstep"] }
   ]
   ```

2. **Is EventBus subscription correct?**
   ```csharp
   // Correct:
   EventBus.Subscribe<AnimationFrameEvent>(OnAnimationFrameEvent);

   // Method signature:
   private void OnAnimationFrameEvent(AnimationFrameEvent evt)
   {
       if (evt.Entity != Entity) return; // Filter by entity!
       // Handle event
   }
   ```

3. **Is animation playing?**
   - Events only fire when animation is actively playing
   - Scrubbing manually does NOT fire events

---

### Issue 4: Asset Failed to Load

**Symptoms:** "Failed to load animation asset" in console.

**Causes:**

1. **Incorrect file path**
   - Path must be relative to `Assets/` directory
   - Use forward slashes: `Animations/player.anim`
   - NOT: `C:/Projects/Assets/Animations/player.anim`

2. **Invalid JSON format**
   - Validate JSON syntax at jsonlint.com
   - Check for missing commas, brackets
   - Ensure all required fields present

3. **Missing texture atlas**
   - Verify `atlas` path in JSON
   - Check texture file exists: `Assets/Textures/spritesheet.png`

4. **File permissions**
   - Ensure `.anim` file is readable
   - Check file is not locked by another process

---

### Issue 5: Animation Stops Immediately

**Symptoms:** Animation starts but stops on frame 0.

**Checks:**

1. **Loop flag disabled?**
   ```csharp
   var anim = entity.GetComponent<AnimationComponent>();
   anim.Loop = true; // Enable looping
   ```

2. **Clip has only 1 frame?**
   - Single-frame clips will appear static
   - Add more frames to JSON

3. **FPS is 0?**
   ```json
   "fps": 12.0  // Must be > 0
   ```

---

### Issue 6: Memory Leak

**Symptoms:** Memory usage grows over time, especially when loading/unloading scenes.

**Solution:**

Animation assets use reference counting. Ensure proper cleanup:

```csharp
// AnimationSystem handles this automatically:
// - Assets loaded in OnInit()
// - Assets unloaded when component removed
// - Reference count decremented properly

// Manual cleanup if needed:
AnimationAssetManager.Instance.ClearUnusedAssets();
```

**Diagnostic:**
```csharp
int cachedCount = AnimationAssetManager.Instance.GetCachedAssetCount();
Logger.Info($"Cached animation assets: {cachedCount}");
```

---

## Performance Tips

### 1. Share Animation Assets

**Bad:**
```
- Enemy1.anim (100 KB)
- Enemy2.anim (100 KB)  ← Duplicate data!
- Enemy3.anim (100 KB)
Total: 300 KB
```

**Good:**
```
- Enemies.anim (100 KB with all clips)
  - enemy1_idle
  - enemy1_walk
  - enemy2_idle
  - enemy2_walk
Total: 100 KB
```

**Benefit:** All enemies share same cached asset = 1 texture load, 1 JSON parse.

---

### 2. Optimize Spritesheet Layout

**Use Power-of-Two Textures:**
- Good: 512×512, 1024×1024, 2048×2048
- Bad: 500×500, 1200×800

**Pack Efficiently:**
- Use texture packing tools (TexturePacker, Aseprite)
- Minimize empty space
- Keep related animations on same sheet

---

### 3. Limit Animation Count

**Guideline:**
- **100 animated entities:** No problem
- **500 animated entities:** Monitor frame time
- **1000+ entities:** Consider LOD system (disable animations for distant objects)

**Per-frame cost:** ~0.0001ms per entity

---

### 4. Use Frame Events Sparingly

**Performance Impact:**
- Each event = heap allocation + event bus dispatch
- Most frames should have 0 events

**Good Practice:**
```json
"frames": [
  { "rect": [0, 0, 32, 32], "events": [] },           // No events
  { "rect": [32, 0, 32, 32], "events": [] },          // No events
  { "rect": [64, 0, 32, 32], "events": ["footstep"] } // 1 event
]
```

**Bad Practice:**
```json
"frames": [
  { "rect": [0, 0, 32, 32], "events": ["frame1", "update", "check"] }, // Too many!
  { "rect": [32, 0, 32, 32], "events": ["frame2", "update", "check"] }
]
```

---

### 5. Disable Animations for Off-Screen Entities

```csharp
using ECS;
using Engine.Animation;
using Engine.Scene;
using Engine.Scene.Components;

public class AnimationCulling : ScriptableEntity
{
    protected override void OnUpdate(TimeSpan deltaTime)
    {
        var transform = Entity.GetComponent<TransformComponent>();
        var anim = Entity.GetComponent<AnimationComponent>();

        // Check if on-screen (simplified)
        bool isVisible = IsInCameraBounds(transform.Translation);

        if (isVisible && !anim.IsPlaying)
        {
            AnimationController.Resume(Entity);
        }
        else if (!isVisible && anim.IsPlaying)
        {
            AnimationController.Pause(Entity);
        }
    }

    private bool IsInCameraBounds(Vector3 position)
    {
        // TODO: Implement frustum culling
        return true;
    }
}
```

---

### 6. Cache Frequently Accessed Components

**Bad:**
```csharp
protected override void OnUpdate(TimeSpan deltaTime)
{
    // Gets component every frame!
    var anim = Entity.GetComponent<AnimationComponent>();
    // ...
}
```

**Good:**
```csharp
private AnimationComponent _anim;

protected override void OnInit()
{
    _anim = Entity.GetComponent<AnimationComponent>();
}

protected override void OnUpdate(TimeSpan deltaTime)
{
    // Use cached reference
    if (_anim.IsPlaying)
    {
        // ...
    }
}
```

---

## Additional Resources

- **Specification:** `docs/specs/2d-animation-system.md`
- **Module Documentation:** `docs/modules/animation-event-system.md`
- **Code Review:** `docs/code-review/2d-animation-system-review.md`
- **Sample Assets:** `Editor/assets/animations/example-animations.anim`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-31 | Initial release with Phase 1-3 features |

---

**Questions or Issues?** Check the GitHub repository or contact the development team.
