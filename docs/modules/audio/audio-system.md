# Audio System Guide

## Table of Contents
- [Overview](#overview)
- [Core Concepts](#core-concepts)
- [Getting Started](#getting-started)
- [2D Audio](#2d-audio)
- [3D Spatial Audio](#3d-spatial-audio)
- [Scripting Integration](#scripting-integration)
- [Editor Integration](#editor-integration)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)

---

## Overview

The Audio System provides a complete solution for game audio, supporting both 2D (non-spatial) and 3D (spatial) audio playback. Built on OpenAL via Silk.NET, it integrates seamlessly with the ECS architecture.

### What You Can Achieve

- **2D Audio**: Background music, UI sounds, ambient effects
- **3D Spatial Audio**: Positioned sound sources with distance attenuation
- **Dynamic Control**: Volume, pitch, looping, and playback control via scripts
- **Multiple Sources**: Play many sounds simultaneously (limited by OpenAL)
- **Hot Reload**: Works with the engine's hot reload system
- **Editor Integration**: Full inspector support for audio components

### Supported Formats

Currently supported audio formats:
- **WAV**: `.wav` files (via WavLoader)

Additional formats can be added by implementing `IAudioLoader` interface.

---

## Core Concepts

### AudioEngine (Singleton)

The central audio manager. Automatically initialized by the engine.

```csharp
// Access the singleton
AudioEngine.Instance.PlayOneShot("assets/sounds/click.wav", volume: 0.5f);
```

### AudioListenerComponent

Represents the "ear" in the scene - typically attached to the main camera entity.

**Key Properties:**
- `IsActive` - Only one listener should be active per scene

**Usage:**
```csharp
// In Editor: Add AudioListenerComponent to your camera entity
// The listener automatically updates position/orientation based on the camera's transform
```

### AudioSourceComponent

Represents a sound emitter in the scene.

**Key Properties:**
- `AudioClip` - The audio file to play (loaded via path)
- `Volume` - Volume level (0.0 to 1.0+)
- `Pitch` - Pitch multiplier (0.1 to 10.0+, default: 1.0)
- `Loop` - Whether to loop the audio
- `PlayOnAwake` - Automatically play when scene starts
- `Is3D` - Enable 3D spatial audio
- `MinDistance` - Distance where attenuation starts (3D only)
- `MaxDistance` - Maximum audible distance (3D only)

---

## Getting Started

### Step 1: Set Up the Audio Listener

**In the Editor:**

1. Select your main camera entity
2. Add Component → Audio Listener Component
3. Ensure `IsActive` is checked (enabled by default)

**Result:** The audio listener will now follow the camera's position and orientation.

### Step 2: Prepare Audio Files

Place your audio files in your project's asset directory:

```
YourProject/
├── assets/
│   └── sounds/
│       ├── music/
│       │   └── background.wav
│       ├── sfx/
│       │   ├── jump.wav
│       │   ├── footstep.wav
│       │   └── explosion.wav
│       └── ambient/
│           └── wind.wav
```

---

## 2D Audio

2D audio is not affected by position - perfect for UI sounds, music, and ambient effects.

### Example 1: Background Music

**In the Editor:**

1. Create a new entity: "Background Music"
2. Add Component → Audio Source Component
3. Configure:
   - Audio Clip: `assets/sounds/music/background.wav`
   - Volume: `0.6`
   - Loop: ✓ (checked)
   - Play On Awake: ✓ (checked)
   - Is 3D: ☐ (unchecked)

**Result:** Music starts playing when the scene loads and loops continuously.

### Example 2: UI Button Click Sound

**Via Script:**

```csharp
using Engine.Scene;
using Engine.Audio;

public class UIButton : ScriptableEntity
{
    private string clickSoundPath = "assets/sounds/sfx/click.wav";

    public void OnClick()
    {
        // Quick one-shot sound
        AudioEngine.Instance.PlayOneShot(clickSoundPath, volume: 0.7f);
    }
}
```

### Example 3: Controlled Sound Effect

**Via Script with AudioSourceComponent:**

```csharp
using Engine.Scene;
using Engine.Scene.Components;

public class Player : ScriptableEntity
{
    private AudioSourceComponent? jumpSound;

    public override void OnStart()
    {
        // Find or create audio source component
        jumpSound = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (Input.IsKeyPressed(Key.Space) && IsGrounded())
        {
            Jump();

            // Play jump sound
            if (jumpSound != null)
            {
                jumpSound.Play();
            }
        }
    }

    private void Jump() { /* ... */ }
    private bool IsGrounded() { /* ... */ return true; }
}
```

**In the Editor:**

1. Select your Player entity
2. Add Component → Audio Source Component
3. Configure:
   - Audio Clip: `assets/sounds/sfx/jump.wav`
   - Volume: `0.8`
   - Is 3D: ☐ (unchecked)
   - Play On Awake: ☐ (unchecked) - we'll trigger it manually

---

## 3D Spatial Audio

3D audio is positioned in world space with distance attenuation and stereo panning.

### Example 1: Campfire Ambient Sound

**In the Editor:**

1. Create an entity: "Campfire"
2. Position it in 3D space (e.g., Translation: `{0, 0, 0}`)
3. Add Component → Audio Source Component
4. Configure:
   - Audio Clip: `assets/sounds/ambient/fire.wav`
   - Volume: `1.0`
   - Loop: ✓ (checked)
   - Play On Awake: ✓ (checked)
   - Is 3D: ✓ (checked)
   - Min Distance: `5.0` - Full volume within 5 units
   - Max Distance: `50.0` - Inaudible beyond 50 units

**Result:** The fire sound gets louder as you approach and quieter as you move away.

### Example 2: Footstep System with 3D Audio

**Via Script:**

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Numerics;

public class Enemy : ScriptableEntity
{
    private AudioSourceComponent? footstepAudio;
    private float stepInterval = 0.5f; // seconds
    private float timeSinceLastStep = 0f;
    private bool isWalking = false;

    public override void OnStart()
    {
        footstepAudio = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Simple walking logic
        isWalking = IsMoving();

        if (isWalking)
        {
            timeSinceLastStep += (float)deltaTime.TotalSeconds;

            if (timeSinceLastStep >= stepInterval)
            {
                PlayFootstep();
                timeSinceLastStep = 0f;
            }
        }

        // The AudioSystem automatically updates the 3D position
        // based on the entity's TransformComponent!
    }

    private void PlayFootstep()
    {
        if (footstepAudio != null && !footstepAudio.IsPlaying)
        {
            footstepAudio.Play();
        }
    }

    private bool IsMoving()
    {
        // Your movement detection logic
        return true;
    }
}
```

**In the Editor:**

1. Select your Enemy entity
2. Add Component → Audio Source Component
3. Configure:
   - Audio Clip: `assets/sounds/sfx/footstep.wav`
   - Volume: `0.6`
   - Is 3D: ✓ (checked)
   - Min Distance: `2.0` - Loud when close
   - Max Distance: `30.0` - Hear enemies approaching

**Result:** Players can hear enemies approaching based on their position.

### Example 3: Explosion with Dynamic Position

**Via Script:**

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Numerics;

public class Grenade : ScriptableEntity
{
    private float fuseTime = 3.0f;
    private float currentTime = 0f;

    public override void OnUpdate(TimeSpan deltaTime)
    {
        currentTime += (float)deltaTime.TotalSeconds;

        if (currentTime >= fuseTime)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Get current position
        var transform = Entity.GetComponent<TransformComponent>();
        Vector3 explosionPosition = transform.Translation;

        // Play 3D explosion sound at this position
        PlayExplosionSound(explosionPosition);

        // Destroy grenade entity
        // Entity.Destroy(); // Implement based on your entity lifecycle
    }

    private void PlayExplosionSound(Vector3 position)
    {
        // Option 1: Use existing AudioSourceComponent
        var audioSource = Entity.GetComponent<AudioSourceComponent>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Option 2: Create a temporary audio entity
        // (More advanced - see "Dynamic Audio Sources" section below)
    }
}
```

### Understanding Distance Attenuation

The audio system uses OpenAL's inverse distance model:

```
Volume = BaseVolume * (MinDistance / (MinDistance + RolloffFactor * (Distance - MinDistance)))
```

**Guidelines:**
- **MinDistance**: Distance at which sound starts to attenuate
  - Small sources (footsteps): 1-3 units
  - Medium sources (voice, fire): 5-10 units
  - Large sources (explosions): 10-20 units

- **MaxDistance**: Maximum audible distance
  - Quiet sounds: 20-40 units
  - Normal sounds: 40-80 units
  - Loud sounds: 80-150 units

---

## Scripting Integration

### Accessing Audio Components

```csharp
using Engine.Scene;
using Engine.Scene.Components;

public class MyScript : ScriptableEntity
{
    private AudioSourceComponent? audioSource;

    public override void OnStart()
    {
        // Get audio source from this entity
        audioSource = Entity.GetComponent<AudioSourceComponent>();

        // Or find from another entity
        var musicEntity = FindEntityByName("Background Music");
        var musicSource = musicEntity?.GetComponent<AudioSourceComponent>();
    }
}
```

### Playback Control

```csharp
public class AudioController : ScriptableEntity
{
    private AudioSourceComponent? audioSource;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    public void PlaySound()
    {
        audioSource?.Play();
    }

    public void PauseSound()
    {
        audioSource?.Pause();
    }

    public void StopSound()
    {
        audioSource?.Stop();
    }

    public void TogglePlayPause()
    {
        if (audioSource == null) return;

        if (audioSource.IsPlaying)
            audioSource.Pause();
        else
            audioSource.Play();
    }
}
```

### Dynamic Property Changes

```csharp
public class VolumeController : ScriptableEntity
{
    private AudioSourceComponent? audioSource;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Fade in
        if (audioSource != null && audioSource.Volume < 1.0f)
        {
            audioSource.Volume += (float)deltaTime.TotalSeconds * 0.5f; // Fade speed
            audioSource.Volume = Math.Min(audioSource.Volume, 1.0f);
        }
    }

    public void SetPitch(float pitch)
    {
        if (audioSource != null)
        {
            audioSource.Pitch = pitch; // 0.5 = half speed, 2.0 = double speed
        }
    }

    public void ToggleLoop()
    {
        if (audioSource != null)
        {
            audioSource.Loop = !audioSource.Loop;
        }
    }
}
```

### Changing Audio Clips at Runtime

```csharp
using Engine.Audio;

public class MusicManager : ScriptableEntity
{
    private AudioSourceComponent? musicSource;
    private string[] musicTracks = {
        "assets/sounds/music/track1.wav",
        "assets/sounds/music/track2.wav",
        "assets/sounds/music/track3.wav"
    };
    private int currentTrack = 0;

    public override void OnStart()
    {
        musicSource = Entity.GetComponent<AudioSourceComponent>();
        PlayCurrentTrack();
    }

    public void NextTrack()
    {
        currentTrack = (currentTrack + 1) % musicTracks.Length;
        PlayCurrentTrack();
    }

    private void PlayCurrentTrack()
    {
        if (musicSource == null) return;

        // Load new audio clip
        var newClip = AudioEngine.Instance.LoadAudioClip(musicTracks[currentTrack]);

        musicSource.AudioClip = newClip;
        musicSource.Play();
    }
}
```

### Advanced: Dynamic 3D Audio Configuration

```csharp
public class ProximityAudio : ScriptableEntity
{
    private AudioSourceComponent? audioSource;
    private Entity? player;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
        player = FindEntityByName("Player");
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (audioSource == null || player == null) return;

        // Calculate distance to player
        var myTransform = Entity.GetComponent<TransformComponent>();
        var playerTransform = player.GetComponent<TransformComponent>();

        float distance = Vector3.Distance(myTransform.Translation, playerTransform.Translation);

        // Dynamically adjust audio properties based on distance
        if (distance < 10.0f)
        {
            // Player is close - make it 3D
            audioSource.RuntimeAudioSource?.SetSpatialMode(true, minDistance: 2.0f, maxDistance: 30.0f);
        }
        else if (distance > 50.0f)
        {
            // Player is far - disable 3D (performance optimization)
            audioSource.RuntimeAudioSource?.SetSpatialMode(false);
        }
    }

    private Entity? FindEntityByName(string name)
    {
        // Implement entity search logic
        return null;
    }
}
```

---

## Editor Integration

### Adding Audio to Entities

**Method 1: Via Inspector**

1. Select entity in Scene Hierarchy
2. In Properties Panel, click "Add Component"
3. Select "Audio Source Component" or "Audio Listener Component"
4. Configure properties in the inspector

**Method 2: Via Component Selector**

The component selector provides quick access to add audio components to any entity.

### Inspector Controls

**Audio Source Component Inspector:**
- Audio Clip field with file browser
- Volume slider (0.0 - 2.0)
- Pitch slider (0.1 - 3.0)
- Loop checkbox
- Play On Awake checkbox
- Is 3D checkbox
- Min Distance slider (3D only)
- Max Distance slider (3D only)
- Playback controls (Play/Pause/Stop) - appears in play mode

**Audio Listener Component Inspector:**
- Is Active checkbox

### Scene Workflow

**Typical Scene Setup:**

```
Scene
├── Main Camera
│   └── Components
│       ├── TransformComponent
│       ├── CameraComponent
│       └── AudioListenerComponent (IsActive: true)
│
├── Player
│   └── Components
│       ├── TransformComponent
│       ├── AudioSourceComponent (footsteps - 3D, not PlayOnAwake)
│       └── ScriptComponent (PlayerController)
│
├── Background Music
│   └── Components
│       ├── TransformComponent
│       └── AudioSourceComponent (music - 2D, PlayOnAwake, Loop)
│
└── Campfire
    └── Components
        ├── TransformComponent (Translation: {10, 0, 5})
        └── AudioSourceComponent (fire - 3D, PlayOnAwake, Loop)
```

### Testing Audio in Editor

1. **Play Mode**: Click the Play button in the editor toolbar
2. Audio sources with `PlayOnAwake` will start automatically
3. Audio follows the scene camera (which has the AudioListenerComponent)
4. Move the camera to test 3D spatial audio

---

## Performance Considerations

### Best Practices

1. **Audio Clip Caching**
   ```csharp
   // Good - Load once, reuse
   private IAudioClip? cachedClip;

   public override void OnStart()
   {
       cachedClip = AudioEngine.Instance.LoadAudioClip("assets/sounds/sfx/shot.wav");
   }

   public void Shoot()
   {
       AudioEngine.Instance.PlayOneShot("assets/sounds/sfx/shot.wav"); // Uses cache
   }
   ```

2. **Limit Active Sources**
   - Typical games: 16-32 simultaneous sources
   - Use `PlayOneShot` for fire-and-forget sounds
   - Stop/reuse sources for continuous sounds

3. **3D Audio Optimization**
   ```csharp
   // Disable distant 3D sources
   if (distanceToListener > audioSource.MaxDistance * 1.2f)
   {
       audioSource.RuntimeAudioSource?.SetSpatialMode(false); // Disable 3D processing
   }
   ```

4. **Update Frequency**
   - AudioSystem runs at Priority 160 (after scripts, before rendering)
   - Position updates happen every frame automatically
   - Distance properties are updated every frame (optimize if needed)

### Memory Management

- Audio clips are cached by the AudioEngine
- Call `AudioEngine.Instance.UnloadAudioClip(path)` to free memory
- AudioSourceComponent disposes runtime sources on entity destruction
- Use WAV files judiciously - they're uncompressed

---

## Troubleshooting

### No Sound Playing

**Check:**
1. Is there an active AudioListenerComponent in the scene?
   - Only one listener should have `IsActive = true`
2. Is the audio file path correct?
   - Paths are relative to the executable
   - Check console for loading errors
3. Is the volume > 0?
4. Is the AudioSourceComponent's `AudioClip` assigned?

### 3D Audio Not Working

**Check:**
1. Is `Is3D` enabled on the AudioSourceComponent?
2. Does the entity have a TransformComponent?
3. Is the listener (camera) too far away?
   - Check MaxDistance setting
4. Is the audio source positioned correctly in world space?

### Audio Cutting Out

**Possible Causes:**
- Too many simultaneous sources (OpenAL limit reached)
- Audio source being destroyed while playing
- Min/Max distance too restrictive

**Solution:**
```csharp
// Implement audio pooling for frequently played sounds
public class AudioPool
{
    private Queue<AudioSourceComponent> availableSources = new();

    public AudioSourceComponent GetSource()
    {
        if (availableSources.Count > 0)
            return availableSources.Dequeue();

        // Create new source or return null
        return null;
    }

    public void ReturnSource(AudioSourceComponent source)
    {
        source.Stop();
        availableSources.Enqueue(source);
    }
}
```

### Crackling or Distortion

**Check:**
- Volume levels (> 1.0 can cause clipping)
- Pitch values (extreme values can cause artifacts)
- Audio file quality

### Performance Issues

**Optimize:**
1. Reduce number of active 3D sources
2. Increase update intervals for distant sources
3. Use 2D audio where spatial positioning isn't needed
4. Disable audio for off-screen entities

---

## Advanced Examples

### Example: Interactive Music System

```csharp
using Engine.Scene;
using Engine.Scene.Components;

public class AdaptiveMusicSystem : ScriptableEntity
{
    private AudioSourceComponent? ambientLayer;
    private AudioSourceComponent? actionLayer;
    private float targetAmbientVolume = 1.0f;
    private float targetActionVolume = 0.0f;
    private float fadeSpeed = 1.0f;

    public override void OnStart()
    {
        // Setup layers
        var ambientEntity = FindEntityByName("Music_Ambient");
        var actionEntity = FindEntityByName("Music_Action");

        ambientLayer = ambientEntity?.GetComponent<AudioSourceComponent>();
        actionLayer = actionEntity?.GetComponent<AudioSourceComponent>();

        // Start both layers
        ambientLayer?.Play();
        actionLayer?.Play();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;

        // Smooth fade between layers
        if (ambientLayer != null)
        {
            ambientLayer.Volume = Lerp(ambientLayer.Volume, targetAmbientVolume, fadeSpeed * dt);
        }

        if (actionLayer != null)
        {
            actionLayer.Volume = Lerp(actionLayer.Volume, targetActionVolume, fadeSpeed * dt);
        }
    }

    public void EnterCombat()
    {
        targetAmbientVolume = 0.3f;
        targetActionVolume = 1.0f;
    }

    public void ExitCombat()
    {
        targetAmbientVolume = 1.0f;
        targetActionVolume = 0.0f;
    }

    private float Lerp(float a, float b, float t) => a + (b - a) * Math.Min(t, 1.0f);
    private Entity? FindEntityByName(string name) => null; // Implement
}
```

### Example: Audio Occlusion (Simple)

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Numerics;

public class AudioOcclusionSystem : ScriptableEntity
{
    private AudioSourceComponent? audioSource;
    private Entity? listener;
    private float baseVolume = 1.0f;
    private float occludedVolume = 0.3f;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
        listener = FindEntityByName("Main Camera");
        baseVolume = audioSource?.Volume ?? 1.0f;
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (audioSource == null || listener == null) return;

        var sourceTransform = Entity.GetComponent<TransformComponent>();
        var listenerTransform = listener.GetComponent<TransformComponent>();

        // Simple raycast to check for occlusion
        bool isOccluded = IsLineOfSightBlocked(sourceTransform.Translation, listenerTransform.Translation);

        // Adjust volume based on occlusion
        float targetVolume = isOccluded ? occludedVolume : baseVolume;
        audioSource.Volume = Lerp(audioSource.Volume, targetVolume, 5.0f * (float)deltaTime.TotalSeconds);
    }

    private bool IsLineOfSightBlocked(Vector3 from, Vector3 to)
    {
        // Implement physics raycast
        // Return true if there's a wall between source and listener
        return false;
    }

    private float Lerp(float a, float b, float t) => a + (b - a) * Math.Min(t, 1.0f);
    private Entity? FindEntityByName(string name) => null;
}
```

---

## API Reference Quick Guide

### AudioEngine

```csharp
// Singleton access
AudioEngine.Instance

// Methods
void Initialize()
void Shutdown()
IAudioSource CreateAudioSource()
IAudioClip LoadAudioClip(string path)
void UnloadAudioClip(string path)
void PlayOneShot(string clipPath, float volume = 1.0f)
```

### IAudioSource

```csharp
// Playback
void Play()
void Pause()
void Stop()

// Properties
IAudioClip Clip { get; set; }
float Volume { get; set; }              // 0.0 to 1.0+
float Pitch { get; set; }               // 0.1 to 10.0+
bool Loop { get; set; }
bool IsPlaying { get; }
bool IsPaused { get; }
float PlaybackPosition { get; set; }    // In seconds

// Spatial Audio
void SetPosition(Vector3 position)
void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f)
```

### AudioSourceComponent

```csharp
// Properties
IAudioClip? AudioClip { get; set; }
float Volume { get; set; }
float Pitch { get; set; }
bool Loop { get; set; }
bool PlayOnAwake { get; set; }
bool Is3D { get; set; }
float MinDistance { get; set; }
float MaxDistance { get; set; }
bool IsPlaying { get; }

// Methods (via RuntimeAudioSource)
void Play()
void Pause()
void Stop()
```

### AudioListenerComponent

```csharp
// Properties
bool IsActive { get; set; }
```

---

## Summary

The Audio System provides a powerful, easy-to-use solution for game audio:

✅ **2D Audio** for music, UI, and ambient effects
✅ **3D Spatial Audio** with distance attenuation
✅ **Full Script Control** for dynamic audio
✅ **Editor Integration** with visual component editors
✅ **Performance Optimized** with automatic position updates

**Next Steps:**
1. Add an AudioListenerComponent to your camera
2. Create audio source entities for your sounds
3. Experiment with 2D and 3D audio settings
4. Write scripts to control audio dynamically
5. Build immersive audio experiences!

For questions or issues, check the [Troubleshooting](#troubleshooting) section or review the source code in `Engine/Scene/Systems/AudioSystem.cs`.
