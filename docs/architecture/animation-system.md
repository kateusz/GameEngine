# Animation System

Sprite animation via `AnimationAsset` files containing named clips with atlas-based frames. `AnimationSystem` runs at priority 140 (after scripts at 110, before rendering at 150+).

## Component Diagram

```mermaid
graph TD
    ANS[AnimationSystem<br/>Priority: 140] -->|reads/writes| AC[AnimationComponent]
    ANS -->|writes| STRC[SubTextureRendererComponent<br/>TexCoords + Texture]
    ANS -->|loads via| IAAM[IAnimationAssetManager<br/>ref-counted cache]
    ANS -->|publishes| EB[EventBus]

    IAAM -->|returns| AA[AnimationAsset]
    AA -->|contains| CLIP[AnimationClip[]]
    CLIP -->|contains| AF[AnimationFrame[]]
    AF -->|has| UV[Pre-calculated TexCoords]
    AF -->|has| EVT[Frame Events]

    EB -->|dispatches| AFE[AnimationFrameEvent]
    EB -->|dispatches| ACE[AnimationCompleteEvent]

    SRS[SpriteRenderSystem<br/>Priority: 150] -->|reads| STRC
    STRS[SubTextureRenderSystem<br/>Priority: 160] -->|reads| STRC

    style ANS fill:#4a90d9,color:#fff
    style AC fill:#5cb85c,color:#fff
    style STRC fill:#5cb85c,color:#fff
    style IAAM fill:#f0ad4e,color:#fff
    style EB fill:#d9534f,color:#fff
```

## Components

### AnimationComponent

| Property | Type | Default | Serialized | Description |
|---|---|---|---|---|
| `AssetPath` | `string?` | null | Yes | Path to animation JSON file (relative to Assets/) |
| `Asset` | `AnimationAsset?` | null | No | Runtime-loaded asset reference |
| `CurrentClipName` | `string` | "" | Yes | Active clip name (e.g., "idle", "walk") |
| `IsPlaying` | `bool` | false | No | Playback state |
| `Loop` | `bool` | true | Yes | Loop current clip (overridable, initialized from clip default) |
| `PlaybackSpeed` | `float` | 1.0 | Yes | Speed multiplier |
| `CurrentFrameIndex` | `int` | 0 | No | Current frame (0-based) |
| `FrameTimer` | `float` | 0.0 | No | Time accumulator within current frame |
| `PreviousFrameIndex` | `int` | -1 | No | Previous frame (for event detection) |
| `ShowDebugInfo` | `bool` | false | Yes | Debug overlay toggle |

## Animation Assets

### AnimationAsset (record, IDisposable)

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | Unique asset identifier |
| `AtlasPath` | `string` | Relative path to texture atlas |
| `Atlas` | `Texture2D` | Loaded atlas texture (runtime, not serialized) |
| `CellSize` | `Vector2` | Grid cell dimensions in pixels |
| `Clips` | `AnimationClip[]` | Named animation clips |

### AnimationClip (record)

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Clip identifier (e.g., "idle", "attack") |
| `Fps` | `float` | Playback framerate |
| `Loop` | `bool` | Default loop behavior |
| `Frames` | `AnimationFrame[]` | Ordered frame sequence |
| `Duration` | `float` | Calculated: `Frames.Length / Fps` |
| `FrameDuration` | `float` | Calculated: `1.0 / Fps` |

### AnimationFrame (record)

| Property | Type | Description |
|---|---|---|
| `Rect` | `Rectangle` | Pixel rectangle [x, y, w, h] in atlas |
| `Pivot` | `Vector2` | Normalized pivot [0..1], default (0.5, 0.0) |
| `Flip` | `Vector2?` | Horizontal/vertical flip flags |
| `Rotation` | `float?` | Degrees (clockwise) |
| `Scale` | `Vector2` | Per-frame scale, default (1,1) |
| `Events` | `string[]` | Event names fired on frame entry |
| `TexCoords` | `Vector2[4]` | Pre-calculated UVs [BL, BR, TR, TL] |

UV coordinates are pre-calculated during asset load via `CalculateUvCoords()` to avoid per-frame computation. Flip flags are baked into the UVs.

## Animation Pipeline

```mermaid
sequenceDiagram
    participant Loop as Game Loop
    participant ANS as AnimationSystem
    participant IAAM as IAnimationAssetManager
    participant AC as AnimationComponent
    participant STRC as SubTextureRendererComponent
    participant EB as EventBus

    Loop->>ANS: OnUpdate(deltaTime)
    ANS->>ANS: Iterate context.View<AnimationComponent>()

    alt Asset == null && AssetPath set
        ANS->>IAAM: LoadAsset(assetPath)
        IAAM-->>ANS: AnimationAsset (cached, ref-counted)
        ANS->>AC: Set Asset, default clip, loop from clip
    end

    alt IsPlaying == false
        Note over ANS: Skip this entity
    else IsPlaying == true
        ANS->>AC: Store PreviousFrameIndex
        ANS->>AC: FrameTimer += deltaTime * PlaybackSpeed

        loop While FrameTimer >= FrameDuration
            ANS->>AC: FrameTimer -= FrameDuration
            ANS->>AC: CurrentFrameIndex++

            alt CurrentFrameIndex >= Frames.Length
                alt Loop == true
                    ANS->>AC: CurrentFrameIndex = 0
                else Loop == false
                    ANS->>AC: Clamp to last frame, IsPlaying = false
                    ANS->>EB: Publish(AnimationCompleteEvent)
                end
            end

            opt Frame has Events[]
                ANS->>EB: Publish(AnimationFrameEvent) per event
            end
        end

        Note over ANS: Update renderer
        ANS->>STRC: Texture = asset.Atlas
        ANS->>STRC: TexCoords = currentFrame.TexCoords
    end
```

## Integration with Rendering

The system execution order ensures animation data is ready before rendering:

| Priority | System | Role |
|---|---|---|
| 110 | ScriptUpdateSystem | Scripts may trigger clip changes |
| **140** | **AnimationSystem** | **Advances frames, updates UV coords** |
| 145 | PrimaryCameraSystem | Camera setup |
| 150 | SpriteRenderSystem | Renders sprites |
| 160 | SubTextureRenderSystem | Renders sub-texture quads |

`AnimationSystem` writes to `SubTextureRendererComponent` (texture reference + UV coords). It contains zero rendering logic -- pure data mutation following ECS principles.

## Asset Management

`IAnimationAssetManager` provides reference-counted caching:
- `LoadAsset(path)` -- returns cached asset or loads from JSON, increments ref count
- `UnloadAsset(path)` -- decrements ref count, disposes at zero
- `ClearUnusedAssets()` -- removes all zero-ref assets (call after scene changes)
- `ClearAllAssets()` -- force unload everything (call on scene unload)
- Factory owns texture lifetime; `AnimationAsset.Dispose()` only nulls the atlas reference

## Event System

Two event types published via `EventBus`:

**AnimationFrameEvent** -- dispatched when entering a frame that has `Events[]` defined:
- `Entity`, `ClipName`, `EventName`, `FrameIndex`, `Frame`
- Use cases: footstep sounds, hit detection triggers, particle spawns

**AnimationCompleteEvent** -- dispatched when a non-looping clip finishes:
- `Entity`, `ClipName`
- Use case: transition to next animation state

## Key Files

| File | Purpose |
|---|---|
| `Engine/Scene/Systems/AnimationSystem.cs` | ECS system (priority 140) |
| `Engine/Scene/Components/AnimationComponent.cs` | Animation state component |
| `Engine/Animation/AnimationAsset.cs` | Asset record (atlas + clips) |
| `Engine/Animation/AnimationClip.cs` | Clip record (name, fps, frames) |
| `Engine/Animation/AnimationFrame.cs` | Frame record (rect, UVs, events) |
| `Engine/Animation/IAnimationAssetManager.cs` | Ref-counted asset cache interface |
| `Engine/Animation/AnimationAssetManager.cs` | Asset manager implementation |
| `Engine/Animation/Events/AnimationFrameEvent.cs` | Frame event |
| `Engine/Animation/Events/AnimationCompleteEvent.cs` | Completion event |
| `Engine/Events/EventBus.cs` | Pub/sub event bus |
