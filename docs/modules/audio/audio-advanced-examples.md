# Advanced Audio System Examples

This document contains advanced patterns and complete examples for sophisticated audio implementations.

## Table of Contents
- [Audio Pool System](#audio-pool-system)
- [Adaptive Music System](#adaptive-music-system)
- [Footstep System with Surface Detection](#footstep-system-with-surface-detection)
- [Audio Occlusion System](#audio-occlusion-system)
- [Procedural Audio Events](#procedural-audio-events)
- [Audio Settings Manager](#audio-settings-manager)
- [Performance Monitoring](#performance-monitoring)

---

## Audio Pool System

For games that spawn many temporary sounds (bullets, impacts, etc.), pooling prevents garbage collection overhead.

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Audio;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Manages a pool of reusable audio sources to avoid creating/destroying entities.
/// Use for frequent one-shot sounds like gunshots, impacts, etc.
/// </summary>
public class AudioPoolSystem : ScriptableEntity
{
    private static AudioPoolSystem? _instance;
    public static AudioPoolSystem? Instance => _instance;

    // Pool configuration
    private const int InitialPoolSize = 10;
    private const int MaxPoolSize = 50;

    // Pool storage
    private Queue<PooledAudioSource> availableSources = new();
    private List<PooledAudioSource> activeSources = new();
    private List<Entity> poolEntities = new();

    // Cached clips for performance
    private Dictionary<string, IAudioClip> clipCache = new();

    public override void OnStart()
    {
        _instance = this;
        InitializePool();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Return finished sources to pool
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var source = activeSources[i];
            if (!source.AudioSource.IsPlaying)
            {
                ReturnToPool(source);
                activeSources.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Plays a 2D sound effect from the pool
    /// </summary>
    public void PlaySFX(string clipPath, float volume = 1.0f, float pitch = 1.0f)
    {
        var source = GetFromPool();
        if (source == null) return;

        SetupAudioSource(source, clipPath, volume, pitch, false, Vector3.Zero);
        source.AudioSource.Play();
        activeSources.Add(source);
    }

    /// <summary>
    /// Plays a 3D sound effect at a specific position
    /// </summary>
    public void PlaySFX3D(string clipPath, Vector3 position, float volume = 1.0f,
                          float minDistance = 1.0f, float maxDistance = 50.0f)
    {
        var source = GetFromPool();
        if (source == null) return;

        SetupAudioSource(source, clipPath, volume, 1.0f, true, position, minDistance, maxDistance);
        source.AudioSource.Play();
        activeSources.Add(source);
    }

    /// <summary>
    /// Preload audio clips for instant playback
    /// </summary>
    public void PreloadClip(string clipPath)
    {
        if (!clipCache.ContainsKey(clipPath))
        {
            var clip = AudioEngine.Instance.LoadAudioClip(clipPath);
            clipCache[clipPath] = clip;
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < InitialPoolSize; i++)
        {
            CreatePooledSource();
        }
    }

    private PooledAudioSource? GetFromPool()
    {
        if (availableSources.Count == 0)
        {
            if (poolEntities.Count < MaxPoolSize)
            {
                return CreatePooledSource();
            }
            else
            {
                Console.WriteLine("Audio pool exhausted! Consider increasing MaxPoolSize.");
                return null;
            }
        }

        return availableSources.Dequeue();
    }

    private void ReturnToPool(PooledAudioSource source)
    {
        source.AudioSource.Stop();
        source.AudioSource.Volume = 1.0f;
        source.AudioSource.Pitch = 1.0f;
        availableSources.Enqueue(source);
    }

    private PooledAudioSource CreatePooledSource()
    {
        // Create entity for pooled audio source
        // Note: This is pseudocode - adapt to your entity creation API
        var entity = CreateEntity($"PooledAudio_{poolEntities.Count}");
        var audioSource = entity.AddComponent<AudioSourceComponent>();

        var pooled = new PooledAudioSource
        {
            Entity = entity,
            AudioSource = audioSource,
            Transform = entity.GetComponent<TransformComponent>()
        };

        poolEntities.Add(entity);
        availableSources.Enqueue(pooled);

        return pooled;
    }

    private void SetupAudioSource(PooledAudioSource source, string clipPath, float volume,
                                  float pitch, bool is3D, Vector3 position,
                                  float minDistance = 1.0f, float maxDistance = 50.0f)
    {
        // Set clip
        if (!clipCache.ContainsKey(clipPath))
        {
            PreloadClip(clipPath);
        }
        source.AudioSource.AudioClip = clipCache[clipPath];

        // Set properties
        source.AudioSource.Volume = volume;
        source.AudioSource.Pitch = pitch;
        source.AudioSource.Loop = false;

        // Setup 3D if needed
        if (is3D)
        {
            source.Transform.Translation = position;
            source.AudioSource.RuntimeAudioSource?.SetSpatialMode(true, minDistance, maxDistance);
            source.AudioSource.RuntimeAudioSource?.SetPosition(position);
        }
        else
        {
            source.AudioSource.RuntimeAudioSource?.SetSpatialMode(false);
        }
    }

    private Entity CreateEntity(string name)
    {
        // Implement entity creation based on your API
        // This is a placeholder
        return null!;
    }

    private class PooledAudioSource
    {
        public Entity Entity { get; set; } = null!;
        public AudioSourceComponent AudioSource { get; set; } = null!;
        public TransformComponent Transform { get; set; } = null!;
    }
}
```

**Usage:**
```csharp
// Preload common sounds at game start
AudioPoolSystem.Instance?.PreloadClip("assets/sounds/sfx/gunshot.wav");
AudioPoolSystem.Instance?.PreloadClip("assets/sounds/sfx/impact.wav");

// Play pooled sounds
AudioPoolSystem.Instance?.PlaySFX("assets/sounds/sfx/gunshot.wav", volume: 0.8f);
AudioPoolSystem.Instance?.PlaySFX3D("assets/sounds/sfx/explosion.wav", explosionPosition, volume: 1.5f);
```

---

## Adaptive Music System

Dynamically crossfade between music layers based on game state.

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Collections.Generic;

/// <summary>
/// Manages adaptive music with smooth crossfading between layers and tracks.
/// Perfect for dynamic game soundtracks that respond to gameplay.
/// </summary>
public class AdaptiveMusicSystem : ScriptableEntity
{
    private static AdaptiveMusicSystem? _instance;
    public static AdaptiveMusicSystem? Instance => _instance;

    // Music layers
    private Dictionary<string, MusicLayer> layers = new();
    private string currentState = "calm";

    // Crossfade settings
    private float crossfadeDuration = 2.0f;
    private float defaultVolume = 0.7f;

    public override void OnStart()
    {
        _instance = this;
        InitializeLayers();
        SetMusicState("calm");
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;

        // Update all layers
        foreach (var layer in layers.Values)
        {
            layer.Update(dt);
        }
    }

    /// <summary>
    /// Changes music state with smooth crossfade
    /// </summary>
    public void SetMusicState(string state)
    {
        if (currentState == state) return;

        currentState = state;

        // Configure target volumes for each layer based on state
        switch (state)
        {
            case "calm":
                SetLayerTarget("ambient", defaultVolume);
                SetLayerTarget("tension", 0.0f);
                SetLayerTarget("combat", 0.0f);
                break;

            case "tension":
                SetLayerTarget("ambient", defaultVolume * 0.5f);
                SetLayerTarget("tension", defaultVolume);
                SetLayerTarget("combat", 0.0f);
                break;

            case "combat":
                SetLayerTarget("ambient", defaultVolume * 0.3f);
                SetLayerTarget("tension", defaultVolume * 0.5f);
                SetLayerTarget("combat", defaultVolume);
                break;
        }
    }

    /// <summary>
    /// Immediately stops all music
    /// </summary>
    public void StopAllMusic()
    {
        foreach (var layer in layers.Values)
        {
            layer.SetTarget(0.0f);
            layer.AudioSource?.Stop();
        }
    }

    /// <summary>
    /// Sets master music volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        defaultVolume = Math.Clamp(volume, 0.0f, 1.0f);
        // Reapply current state to update volumes
        var temp = currentState;
        currentState = "";
        SetMusicState(temp);
    }

    private void InitializeLayers()
    {
        // Create layer entities
        // Note: Adapt this to your entity creation API
        CreateMusicLayer("ambient", "assets/music/ambient_layer.wav");
        CreateMusicLayer("tension", "assets/music/tension_layer.wav");
        CreateMusicLayer("combat", "assets/music/combat_layer.wav");

        // Start all layers playing (at zero volume)
        foreach (var layer in layers.Values)
        {
            if (layer.AudioSource != null)
            {
                layer.AudioSource.Volume = 0.0f;
                layer.AudioSource.Loop = true;
                layer.AudioSource.Play();
            }
        }
    }

    private void CreateMusicLayer(string layerName, string clipPath)
    {
        // Create entity for this layer
        var entity = CreateEntity($"Music_{layerName}");
        var audioSource = entity?.GetComponent<AudioSourceComponent>();

        if (audioSource != null)
        {
            var clip = AudioEngine.Instance.LoadAudioClip(clipPath);
            audioSource.AudioClip = clip;
            audioSource.Loop = true;
            audioSource.Volume = 0.0f;

            layers[layerName] = new MusicLayer
            {
                Name = layerName,
                AudioSource = audioSource,
                CurrentVolume = 0.0f,
                TargetVolume = 0.0f,
                FadeSpeed = 1.0f / crossfadeDuration
            };
        }
    }

    private void SetLayerTarget(string layerName, float targetVolume)
    {
        if (layers.TryGetValue(layerName, out var layer))
        {
            layer.SetTarget(targetVolume);
        }
    }

    private Entity? CreateEntity(string name)
    {
        // Implement based on your API
        return null;
    }

    private class MusicLayer
    {
        public string Name { get; set; } = "";
        public AudioSourceComponent? AudioSource { get; set; }
        public float CurrentVolume { get; set; }
        public float TargetVolume { get; set; }
        public float FadeSpeed { get; set; }

        public void SetTarget(float target)
        {
            TargetVolume = Math.Clamp(target, 0.0f, 1.0f);
        }

        public void Update(float deltaTime)
        {
            if (AudioSource == null) return;

            // Smooth fade
            if (Math.Abs(CurrentVolume - TargetVolume) > 0.001f)
            {
                float step = FadeSpeed * deltaTime;
                CurrentVolume = MoveTowards(CurrentVolume, TargetVolume, step);
                AudioSource.Volume = CurrentVolume;
            }
        }

        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
                return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }
}
```

**Usage in Game:**
```csharp
// In game events
public void OnEnemySpotted()
{
    AdaptiveMusicSystem.Instance?.SetMusicState("tension");
}

public void OnCombatStart()
{
    AdaptiveMusicSystem.Instance?.SetMusicState("combat");
}

public void OnCombatEnd()
{
    AdaptiveMusicSystem.Instance?.SetMusicState("calm");
}
```

---

## Footstep System with Surface Detection

Plays different footstep sounds based on the surface type.

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Collections.Generic;

/// <summary>
/// Plays contextual footstep sounds based on surface type and movement speed
/// </summary>
public class FootstepSystem : ScriptableEntity
{
    // Surface-specific footstep sounds
    private Dictionary<SurfaceType, string[]> footstepSounds = new()
    {
        { SurfaceType.Grass, new[] {
            "assets/sounds/footsteps/grass_1.wav",
            "assets/sounds/footsteps/grass_2.wav",
            "assets/sounds/footsteps/grass_3.wav"
        }},
        { SurfaceType.Stone, new[] {
            "assets/sounds/footsteps/stone_1.wav",
            "assets/sounds/footsteps/stone_2.wav",
            "assets/sounds/footsteps/stone_3.wav"
        }},
        { SurfaceType.Wood, new[] {
            "assets/sounds/footsteps/wood_1.wav",
            "assets/sounds/footsteps/wood_2.wav"
        }},
        { SurfaceType.Metal, new[] {
            "assets/sounds/footsteps/metal_1.wav",
            "assets/sounds/footsteps/metal_2.wav"
        }}
    };

    private AudioSourceComponent? footstepAudio;
    private TransformComponent? transform;
    private Random random = new Random();

    // Timing
    private float walkStepInterval = 0.5f;  // seconds between steps when walking
    private float runStepInterval = 0.3f;   // seconds between steps when running
    private float timeSinceLastStep = 0f;

    // Movement tracking
    private Vector3 lastPosition;
    private bool isMoving = false;
    private bool isRunning = false;

    // Current surface
    private SurfaceType currentSurface = SurfaceType.Grass;

    public override void OnStart()
    {
        footstepAudio = Entity.GetComponent<AudioSourceComponent>();
        transform = Entity.GetComponent<TransformComponent>();
        lastPosition = transform?.Translation ?? Vector3.Zero;
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (transform == null || footstepAudio == null) return;

        float dt = (float)deltaTime.TotalSeconds;

        // Detect movement
        Vector3 currentPosition = transform.Translation;
        float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
        isMoving = distanceMoved > 0.01f;
        lastPosition = currentPosition;

        // Detect surface type (simplified - implement proper ground detection)
        currentSurface = DetectSurfaceType();

        // Play footsteps if moving
        if (isMoving)
        {
            timeSinceLastStep += dt;
            float stepInterval = isRunning ? runStepInterval : walkStepInterval;

            if (timeSinceLastStep >= stepInterval)
            {
                PlayFootstep();
                timeSinceLastStep = 0f;
            }
        }
        else
        {
            timeSinceLastStep = 0f;
        }
    }

    public void SetRunning(bool running)
    {
        isRunning = running;
    }

    private void PlayFootstep()
    {
        if (footstepAudio == null) return;

        // Get random sound for current surface
        string[] sounds = footstepSounds[currentSurface];
        string soundPath = sounds[random.Next(sounds.Length)];

        // Randomize pitch for variety
        float pitchVariation = 0.9f + (float)random.NextDouble() * 0.2f; // 0.9 to 1.1
        footstepAudio.Pitch = pitchVariation;

        // Adjust volume based on movement speed
        float volumeMultiplier = isRunning ? 1.0f : 0.7f;
        footstepAudio.Volume = 0.6f * volumeMultiplier;

        // Load and play
        var clip = AudioEngine.Instance.LoadAudioClip(soundPath);
        footstepAudio.AudioClip = clip;
        footstepAudio.Play();
    }

    private SurfaceType DetectSurfaceType()
    {
        // Implement proper ground detection using raycasts
        // This is a simplified example
        if (transform == null) return SurfaceType.Grass;

        // Raycast down to detect surface
        // Vector3 rayOrigin = transform.Translation;
        // RaycastHit hit = Physics.Raycast(rayOrigin, Vector3.Down, maxDistance: 2.0f);
        // return hit.Collider?.SurfaceType ?? SurfaceType.Grass;

        // For now, return based on Y position (example)
        float y = transform.Translation.Y;
        if (y < -5.0f) return SurfaceType.Stone;
        if (y > 5.0f) return SurfaceType.Wood;
        return SurfaceType.Grass;
    }

    private enum SurfaceType
    {
        Grass,
        Stone,
        Wood,
        Metal
    }
}
```

**Setup in Editor:**
1. Add AudioSourceComponent to player
2. Set Is3D based on preference (3D for immersion, 2D for consistent volume)
3. Add FootstepSystem script component

---

## Audio Occlusion System

Reduces volume of sounds behind walls using simple raycasting.

```csharp
using Engine.Scene;
using Engine.Scene.Components;
using System.Numerics;

/// <summary>
/// Simple audio occlusion system that reduces volume of sounds blocked by geometry
/// </summary>
public class AudioOcclusionController : ScriptableEntity
{
    private AudioSourceComponent? audioSource;
    private TransformComponent? sourceTransform;
    private Entity? listener;

    // Occlusion settings
    private float baseVolume = 1.0f;
    private float occludedVolumeMultiplier = 0.3f;  // 30% volume when occluded
    private float smoothSpeed = 5.0f;

    private bool wasOccluded = false;

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
        sourceTransform = Entity.GetComponent<TransformComponent>();
        baseVolume = audioSource?.Volume ?? 1.0f;

        // Find listener (camera)
        listener = FindEntityByName("Main Camera");
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        if (audioSource == null || sourceTransform == null || listener == null) return;

        var listenerTransform = listener.GetComponent<TransformComponent>();
        if (listenerTransform == null) return;

        // Check if line of sight is blocked
        bool isOccluded = CheckOcclusion(sourceTransform.Translation, listenerTransform.Translation);

        // Calculate target volume
        float targetVolume = isOccluded ? baseVolume * occludedVolumeMultiplier : baseVolume;

        // Smooth transition
        float dt = (float)deltaTime.TotalSeconds;
        audioSource.Volume = Lerp(audioSource.Volume, targetVolume, smoothSpeed * dt);

        // Optional: Apply low-pass filter effect when occluded
        if (isOccluded && !wasOccluded)
        {
            OnBecameOccluded();
        }
        else if (!isOccluded && wasOccluded)
        {
            OnBecameVisible();
        }

        wasOccluded = isOccluded;
    }

    private bool CheckOcclusion(Vector3 sourcePos, Vector3 listenerPos)
    {
        // Implement physics raycast
        // Example pseudocode:
        // Vector3 direction = Vector3.Normalize(listenerPos - sourcePos);
        // float distance = Vector3.Distance(sourcePos, listenerPos);
        // RaycastHit hit = Physics.Raycast(sourcePos, direction, distance);
        // return hit.Collider != null && hit.Collider.Tag == "Wall";

        // Placeholder: return false for now
        return false;
    }

    private void OnBecameOccluded()
    {
        // Could reduce pitch slightly to simulate muffling
        if (audioSource != null)
        {
            audioSource.Pitch = 0.95f;
        }
    }

    private void OnBecameVisible()
    {
        // Restore pitch
        if (audioSource != null)
        {
            audioSource.Pitch = 1.0f;
        }
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
    }

    private Entity? FindEntityByName(string name)
    {
        // Implement entity search
        return null;
    }
}
```

---

## Procedural Audio Events

Generate procedural audio effects based on game events.

```csharp
using Engine.Scene;
using Engine.Scene.Components;

/// <summary>
/// Generates procedural audio effects by manipulating pitch, volume, and playback
/// </summary>
public class ProceduralAudioGenerator : ScriptableEntity
{
    private AudioSourceComponent? audioSource;
    private Random random = new Random();

    public override void OnStart()
    {
        audioSource = Entity.GetComponent<AudioSourceComponent>();
    }

    /// <summary>
    /// Plays an engine sound that increases in pitch with RPM
    /// </summary>
    public void PlayEngineSound(float rpm, float maxRPM = 8000f)
    {
        if (audioSource == null) return;

        // Map RPM to pitch (0.5 to 2.0)
        float normalizedRPM = Math.Clamp(rpm / maxRPM, 0.0f, 1.0f);
        float pitch = 0.5f + (normalizedRPM * 1.5f);

        // Map RPM to volume
        float volume = 0.3f + (normalizedRPM * 0.7f);

        audioSource.Pitch = pitch;
        audioSource.Volume = volume;

        if (!audioSource.IsPlaying)
        {
            audioSource.Play();
        }
    }

    /// <summary>
    /// Creates a procedural impact sound by layering and pitch-shifting
    /// </summary>
    public void PlayImpact(float impactForce)
    {
        if (audioSource == null) return;

        // Stronger impacts have lower pitch and higher volume
        float normalizedForce = Math.Clamp(impactForce / 100.0f, 0.0f, 1.0f);

        audioSource.Pitch = 1.2f - (normalizedForce * 0.7f);  // 1.2 to 0.5
        audioSource.Volume = 0.5f + (normalizedForce * 0.5f);  // 0.5 to 1.0

        audioSource.Play();
    }

    /// <summary>
    /// Creates a randomized UI sound
    /// </summary>
    public void PlayUISound(UIEventType eventType)
    {
        if (audioSource == null) return;

        switch (eventType)
        {
            case UIEventType.ButtonHover:
                audioSource.Pitch = 1.0f + ((float)random.NextDouble() * 0.2f - 0.1f);
                audioSource.Volume = 0.3f;
                break;

            case UIEventType.ButtonClick:
                audioSource.Pitch = 0.9f + ((float)random.NextDouble() * 0.2f);
                audioSource.Volume = 0.6f;
                break;

            case UIEventType.Error:
                audioSource.Pitch = 0.7f;
                audioSource.Volume = 0.8f;
                break;

            case UIEventType.Success:
                audioSource.Pitch = 1.2f;
                audioSource.Volume = 0.7f;
                break;
        }

        audioSource.Play();
    }

    public enum UIEventType
    {
        ButtonHover,
        ButtonClick,
        Error,
        Success
    }
}
```

---

## Audio Settings Manager

Persistent audio settings with volume sliders and mute toggles.

```csharp
using Engine.Scene;
using System.IO;
using System.Text.Json;

/// <summary>
/// Manages global audio settings with persistence
/// </summary>
public class AudioSettingsManager : ScriptableEntity
{
    private static AudioSettingsManager? _instance;
    public static AudioSettingsManager? Instance => _instance;

    // Settings
    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 0.8f;
    public float SFXVolume { get; private set; } = 1.0f;
    public bool IsMuted { get; private set; } = false;

    private const string SettingsFilePath = "audio_settings.json";
    private float volumeBeforeMute = 1.0f;

    public override void OnStart()
    {
        _instance = this;
        LoadSettings();
    }

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
        SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
        SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Math.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
        SaveSettings();
    }

    public void ToggleMute()
    {
        if (IsMuted)
        {
            Unmute();
        }
        else
        {
            Mute();
        }
    }

    public void Mute()
    {
        if (IsMuted) return;

        volumeBeforeMute = MasterVolume;
        MasterVolume = 0.0f;
        IsMuted = true;
        ApplyVolumeSettings();
    }

    public void Unmute()
    {
        if (!IsMuted) return;

        MasterVolume = volumeBeforeMute;
        IsMuted = false;
        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        // Apply to music system
        AdaptiveMusicSystem.Instance?.SetMasterVolume(MusicVolume * MasterVolume);

        // Apply to audio pool (if using)
        // AudioPoolSystem would need a similar SetMasterVolume method

        Console.WriteLine($"Audio Settings: Master={MasterVolume:F2}, Music={MusicVolume:F2}, SFX={SFXVolume:F2}, Muted={IsMuted}");
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AudioSettings
            {
                MasterVolume = MasterVolume,
                MusicVolume = MusicVolume,
                SFXVolume = SFXVolume,
                IsMuted = IsMuted
            };

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save audio settings: {ex.Message}");
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AudioSettings>(json);

                if (settings != null)
                {
                    MasterVolume = settings.MasterVolume;
                    MusicVolume = settings.MusicVolume;
                    SFXVolume = settings.SFXVolume;
                    IsMuted = settings.IsMuted;

                    ApplyVolumeSettings();
                    Console.WriteLine("Audio settings loaded successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load audio settings: {ex.Message}");
        }
    }

    [Serializable]
    private class AudioSettings
    {
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SFXVolume { get; set; }
        public bool IsMuted { get; set; }
    }
}
```

**Usage in Settings Menu:**
```csharp
// In your settings UI script
public class SettingsMenu : ScriptableEntity
{
    public void OnMasterVolumeSliderChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMasterVolume(value);
    }

    public void OnMusicVolumeSliderChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMusicVolume(value);
    }

    public void OnSFXVolumeSliderChanged(float value)
    {
        AudioSettingsManager.Instance?.SetSFXVolume(value);
    }

    public void OnMuteButtonClicked()
    {
        AudioSettingsManager.Instance?.ToggleMute();
    }
}
```

---

## Performance Monitoring

Track audio system performance and active sources.

```csharp
using Engine.Scene;
using System.Collections.Generic;

/// <summary>
/// Monitors audio system performance
/// </summary>
public class AudioPerformanceMonitor : ScriptableEntity
{
    private static AudioPerformanceMonitor? _instance;
    public static AudioPerformanceMonitor? Instance => _instance;

    // Statistics
    public int ActiveSources { get; private set; }
    public int TotalSources { get; private set; }
    public int LoadedClips { get; private set; }
    public float TotalAudioMemoryMB { get; private set; }

    private float updateInterval = 1.0f; // Update stats every second
    private float timeSinceUpdate = 0f;

    public override void OnStart()
    {
        _instance = this;
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        timeSinceUpdate += (float)deltaTime.TotalSeconds;

        if (timeSinceUpdate >= updateInterval)
        {
            UpdateStatistics();
            timeSinceUpdate = 0f;
        }
    }

    private void UpdateStatistics()
    {
        // Count active audio sources
        // This is pseudocode - implement based on your ECS API
        // var allAudioSources = Context.Instance.View<AudioSourceComponent>();
        // ActiveSources = allAudioSources.Count(s => s.Component.IsPlaying);
        // TotalSources = allAudioSources.Count();

        // Display in console or UI
        if (ActiveSources > 20)
        {
            Console.WriteLine($"Warning: High number of active audio sources: {ActiveSources}");
        }
    }

    public void LogPerformanceReport()
    {
        Console.WriteLine("=== Audio Performance Report ===");
        Console.WriteLine($"Active Sources: {ActiveSources} / {TotalSources}");
        Console.WriteLine($"Loaded Clips: {LoadedClips}");
        Console.WriteLine($"Est. Memory: {TotalAudioMemoryMB:F2} MB");
        Console.WriteLine("===============================");
    }
}
```

---

## Complete Game Example

Putting it all together in a game:

```csharp
using Engine.Scene;
using System.Numerics;

/// <summary>
/// Complete game example using advanced audio systems
/// </summary>
public class GameController : ScriptableEntity
{
    public override void OnStart()
    {
        // Initialize audio systems
        InitializeAudioSystems();

        // Load and start music
        AdaptiveMusicSystem.Instance?.SetMusicState("calm");

        // Preload common sounds
        PreloadCommonSounds();
    }

    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Game logic...
    }

    private void InitializeAudioSystems()
    {
        // Audio systems are typically added as entities in the scene
        // They auto-initialize via OnStart()
    }

    private void PreloadCommonSounds()
    {
        var pool = AudioPoolSystem.Instance;
        pool?.PreloadClip("assets/sounds/sfx/gunshot.wav");
        pool?.PreloadClip("assets/sounds/sfx/impact.wav");
        pool?.PreloadClip("assets/sounds/sfx/explosion.wav");
        pool?.PreloadClip("assets/sounds/ui/click.wav");
    }

    // Game events
    public void OnPlayerShoot(Vector3 position)
    {
        AudioPoolSystem.Instance?.PlaySFX3D(
            "assets/sounds/sfx/gunshot.wav",
            position,
            volume: 0.8f,
            minDistance: 5.0f,
            maxDistance: 100.0f
        );
    }

    public void OnEnemySpotted()
    {
        AdaptiveMusicSystem.Instance?.SetMusicState("tension");
    }

    public void OnCombatStart()
    {
        AdaptiveMusicSystem.Instance?.SetMusicState("combat");
    }

    public void OnPlayerDeath()
    {
        AdaptiveMusicSystem.Instance?.StopAllMusic();
        AudioPoolSystem.Instance?.PlaySFX("assets/sounds/sfx/death.wav", volume: 1.0f);
    }
}
```

---

These advanced examples provide production-ready patterns for professional game audio. Combine and adapt them to create immersive soundscapes for your games!
