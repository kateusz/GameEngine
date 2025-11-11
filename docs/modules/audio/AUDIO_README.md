# Audio System Documentation

Complete documentation for the GameEngine Audio System.

## Quick Links

### Getting Started
1. **5-Minute Setup**: [Quick Start Tutorial](audio-quick-start.md#tutorial-1-adding-background-music-2d-audio)
2. **Understanding the System**: [Core Concepts](audio-system.md#core-concepts)
3. **First Script**: [Scripting Integration](audio-system.md#scripting-integration)

### Common Tasks
- **Add Background Music**: [Quick Start Tutorial 1](audio-quick-start.md#tutorial-1-adding-background-music-2d-audio)
- **Add 3D Sound**: [Quick Start Tutorial 2](audio-quick-start.md#tutorial-2-adding-3d-spatial-audio)
- **Trigger from Code**: [Quick Start Tutorial 3](audio-quick-start.md#tutorial-3-triggering-sounds-from-scripts)
- **Volume Control**: [Audio Settings Manager](audio-advanced-examples.md#audio-settings-manager)
- **Dynamic Music**: [Adaptive Music System](audio-advanced-examples.md#adaptive-music-system)

### Reference
- **API Reference**: [Audio System Guide - API Reference](audio-system.md#api-reference-quick-guide)
- **Troubleshooting**: [Audio System Guide - Troubleshooting](audio-system.md#troubleshooting)
- **Performance**: [Audio System Guide - Performance](audio-system.md#performance-considerations)

---

## Feature Overview

### ✅ What the Audio System Can Do

**2D Audio (Non-Spatial)**
- Background music with looping
- UI sounds (buttons, menus)
- Player sounds (jump, pickup)
- Ambient atmosphere
- Fire-and-forget one-shot sounds

**3D Spatial Audio**
- Position-based sound sources
- Distance attenuation (volume decreases with distance)
- Stereo panning (sound position in left/right channels)
- Listener orientation tracking
- Min/max distance configuration

**Dynamic Control**
- Runtime volume/pitch adjustment
- Play/pause/stop control
- Loop toggling
- Audio clip swapping
- Fade in/out effects

**Performance Features**
- Audio clip caching
- Automatic position updates
- No reflection in hot paths (optimized)
- Configurable update frequency
- Memory-efficient pooling patterns

**Editor Integration**
- Visual component editors
- Audio clip browser
- Real-time property editing
- Play mode testing
- Scene workflow support

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                      Game Scene                              │
│                                                              │
│  ┌──────────────┐         ┌─────────────────────┐          │
│  │ Main Camera  │         │  Audio Source       │          │
│  │ ┌──────────┐ │         │  Entities           │          │
│  │ │Listener  │ │ ◄───────┤  ┌───────────────┐  │          │
│  │ │Component │ │         │  │AudioSource    │  │          │
│  │ └──────────┘ │         │  │Component      │  │          │
│  └──────────────┘         │  └───────────────┘  │          │
│         │                  │  ┌───────────────┐  │          │
│         │                  │  │Transform      │  │          │
│         │                  │  │Component      │  │          │
│         │                  │  └───────────────┘  │          │
│         │                  └─────────────────────┘          │
│         ▼                           │                        │
│  ┌─────────────────────────────────┼───────────────────┐   │
│  │           Audio System (ECS System)                  │   │
│  │  - Updates listener position/orientation            │   │
│  │  - Updates all audio source positions (3D)          │   │
│  │  - Manages playback state                           │   │
│  │  - Priority: 160 (after scripts, before rendering)  │   │
│  └──────────────────────────────────────────────────────┘   │
│                           │                                  │
└───────────────────────────┼──────────────────────────────────┘
                            ▼
                ┌───────────────────────┐
                │   AudioEngine         │
                │   (Singleton)         │
                │  - OpenAL backend     │
                │  - Clip caching       │
                │  - Source creation    │
                └───────────────────────┘
                            │
                            ▼
                ┌───────────────────────┐
                │   OpenAL / Hardware   │
                │   (via Silk.NET)      │
                └───────────────────────┘
```

---

## Workflow Diagram

### Adding Audio to Your Game

```
1. Setup Phase (Once per scene)
   ├─► Add AudioListenerComponent to camera
   └─► Ensure audio files are in assets/sounds/

2. For Each Sound Source
   ├─► Create entity (or use existing)
   ├─► Add AudioSourceComponent
   ├─► Configure properties:
   │   ├─► Audio Clip path
   │   ├─► Volume, Pitch, Loop
   │   ├─► Is3D (for spatial audio)
   │   └─► Min/Max Distance (if 3D)
   └─► Option: Add script for dynamic control

3. Testing
   ├─► Click Play in editor
   ├─► Move camera around (for 3D audio)
   └─► Verify sound behavior

4. Advanced (Optional)
   ├─► Implement audio pooling
   ├─► Create adaptive music system
   ├─► Add audio occlusion
   └─► Build settings/volume UI
```

---

## Code Examples by Use Case

### Use Case: Background Music

```csharp
// In Editor: Add AudioSourceComponent to entity
// Set: Loop=true, PlayOnAwake=true, Is3D=false
```

### Use Case: Button Click Sound

```csharp
AudioEngine.Instance.PlayOneShot("assets/sounds/ui/click.wav", 0.7f);
```

### Use Case: 3D Campfire Ambience

```csharp
// In Editor: Add AudioSourceComponent to entity
// Set: Is3D=true, MinDistance=5, MaxDistance=50
// Position entity at campfire location
---

## Best Practices Summary

### ✅ DO

- **Preload** frequently used audio clips in `OnStart()`
- **Use 2D audio** for UI and non-positional sounds
- **Use 3D audio** for positioned gameplay sounds
- **Pool audio sources** for frequently spawned sounds
- **Cache AudioComponents** in script fields, don't fetch every frame
- **Set appropriate distances** for 3D audio (smaller for quiet sounds)
- **One active listener** per scene (typically on camera)

### ❌ DON'T

- Don't load audio clips in `OnUpdate()` (causes hitches)
- Don't create entities for one-shot sounds (use `PlayOneShot` or pooling)
- Don't set Is3D=true for music/UI sounds
- Don't forget to add AudioListenerComponent to your camera

---

## Troubleshooting Checklist

### No sound at all?
- [ ] AudioListenerComponent exists and `IsActive = true`
- [ ] Audio file path is correct
- [ ] Volume > 0
- [ ] AudioClip is assigned

### 3D audio not working?
- [ ] `Is3D = true` on AudioSourceComponent
- [ ] Entity has TransformComponent
- [ ] Listener within MaxDistance
- [ ] Audio files are properly formatted WAV

### Crackling/distortion?
- [ ] Volume ≤ 1.0 (avoid clipping)
- [ ] Pitch in reasonable range (0.5 - 2.0)
- [ ] Audio file quality is good

---

## File Format Support

### Currently Supported
- ✅ **WAV** (`.wav`) - Uncompressed audio

---