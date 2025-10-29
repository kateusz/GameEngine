using System.Numerics;
using ECS;
using Engine.Audio;
using Engine.Scene.Components;
using Serilog;
using Context = ECS.Context;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for managing audio playback and 3D spatial audio.
/// Handles audio source lifecycle, updates 3D positions, and manages the audio listener.
/// </summary>
public class AudioSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<AudioSystem>();
    
    private readonly IAudioEngine _audioEngine;

    public AudioSystem(IAudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
    }

    /// <summary>
    /// Gets the priority of this system.
    /// Priority 160 ensures audio runs after scripts (150) and before rendering (200+).
    /// </summary>
    public int Priority => 160;

    /// <summary>
    /// Initializes the audio system.
    /// Creates runtime audio sources for all entities with AudioSourceComponent.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("AudioSystem initialized with priority {Priority}", Priority);

        // Create audio sources for all entities that have AudioSourceComponent
        var view = Context.Instance.View<AudioSourceComponent>();
        foreach (var (entity, component) in view)
        {
            InitializeAudioSource(entity, component);
        }
    }

    /// <summary>
    /// Updates the audio system.
    /// Synchronizes audio listener position/orientation and updates 3D audio source positions.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        UpdateListener();
        UpdateAudioSources();
    }

    /// <summary>
    /// Shuts down the audio system.
    /// Cleans up all audio sources.
    /// </summary>
    public void OnShutdown()
    {
        // Clean up all audio sources
        var view = Context.Instance.View<AudioSourceComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.RuntimeAudioSource != null)
            {
                component.RuntimeAudioSource.Dispose();
                component.RuntimeAudioSource = null;
            }
        }

        Logger.Debug("AudioSystem shut down");
    }

    /// <summary>
    /// Plays the audio source for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public static void Play(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning($"Cannot play audio for entity '{entity.Name}' - no AudioSourceComponent found");
            return;
        }

        var component = entity.GetComponent<AudioSourceComponent>();
        if (component.RuntimeAudioSource != null && component.AudioClip != null)
        {
            component.RuntimeAudioSource.Play();
            component.IsPlaying = true;
        }
        else if (component.AudioClip == null)
        {
            Logger.Warning($"Cannot play audio for entity '{entity.Name}' - no AudioClip assigned");
        }
    }

    /// <summary>
    /// Pauses the audio playback for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public static void Pause(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning($"Cannot pause audio for entity '{entity.Name}' - no AudioSourceComponent found");
            return;
        }

        var component = entity.GetComponent<AudioSourceComponent>();
        if (component.RuntimeAudioSource != null)
        {
            component.RuntimeAudioSource.Pause();
            component.IsPlaying = false;
        }
    }

    /// <summary>
    /// Stops the audio playback for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public static void Stop(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning($"Cannot stop audio for entity '{entity.Name}' - no AudioSourceComponent found");
            return;
        }

        var component = entity.GetComponent<AudioSourceComponent>();
        if (component.RuntimeAudioSource != null)
        {
            component.RuntimeAudioSource.Stop();
            component.IsPlaying = false;
        }
    }

    /// <summary>
    /// Initializes an audio source for an entity.
    /// Creates the runtime audio source and sets up initial properties.
    /// </summary>
    private void InitializeAudioSource(Entity entity, AudioSourceComponent component)
    {
        if (component.RuntimeAudioSource != null)
        {
            // Already initialized
            return;
        }

        try
        {
            // Create audio source from engine
            component.RuntimeAudioSource = _audioEngine.CreateAudioSource();

            // Set initial properties
            if (component.AudioClip != null)
            {
                component.RuntimeAudioSource.Clip = component.AudioClip;
            }

            component.RuntimeAudioSource.Volume = component.Volume;
            component.RuntimeAudioSource.Pitch = component.Pitch;
            component.RuntimeAudioSource.Loop = component.Loop;

            // Configure spatial mode
            component.RuntimeAudioSource.SetSpatialMode(component.Is3D, component.MinDistance, component.MaxDistance);

            // Set initial 3D position from transform if applicable
            if (component.Is3D && entity.HasComponent<TransformComponent>())
            {
                var transform = entity.GetComponent<TransformComponent>();
                component.RuntimeAudioSource.SetPosition(transform.Translation);
            }

            // Play on awake if requested
            if (component is { PlayOnAwake: true, AudioClip: not null })
            {
                component.RuntimeAudioSource.Play();
                component.IsPlaying = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to initialize audio source for entity '{entity.Name}' (ID: {entity.Id})");
        }
    }

    /// <summary>
    /// Updates the audio listener position and orientation based on the active AudioListenerComponent.
    /// Typically the listener is attached to the main camera entity.
    /// </summary>
    private void UpdateListener()
    {
        // Find active audio listener with transform
        Entity? activeListenerEntity = null;
        AudioListenerComponent? activeListener = null;

        var listenerView = Context.Instance.View<AudioListenerComponent>();
        foreach (var (entity, component) in listenerView)
        {
            if (component.IsActive && entity.HasComponent<TransformComponent>())
            {
                activeListenerEntity = entity;
                activeListener = component;
                break;
            }
        }

        // Early exit if no active listener found
        if (activeListenerEntity == null || activeListener == null)
            return;

        var transform = activeListenerEntity.GetComponent<TransformComponent>();
        var pos = transform.Translation;

        // Set listener position
        _audioEngine.SetListenerPosition(pos);

        // Set listener orientation based on transform rotation
        var transformMatrix = transform.GetTransform();
        var forward = Vector3.Transform(-Vector3.UnitZ, Quaternion.CreateFromRotationMatrix(transformMatrix));
        var up = Vector3.Transform(Vector3.UnitY, Quaternion.CreateFromRotationMatrix(transformMatrix));

        _audioEngine.SetListenerOrientation(forward, up);
    }

    /// <summary>
    /// Updates all audio sources, synchronizing their properties and 3D positions.
    /// </summary>
    private void UpdateAudioSources()
    {
        var view = Context.Instance.View<AudioSourceComponent>();
        foreach (var (entity, component) in view)
        {
            // Initialize audio source if not already done (for newly added components)
            if (component.RuntimeAudioSource == null)
            {
                InitializeAudioSource(entity, component);
                continue;
            }

            try
            {
                // Update audio source properties
                component.RuntimeAudioSource.Volume = component.Volume;
                component.RuntimeAudioSource.Pitch = component.Pitch;
                component.RuntimeAudioSource.Loop = component.Loop;

                // Update audio clip if changed
                if (component.AudioClip != null && component.RuntimeAudioSource.Clip != component.AudioClip)
                {
                    component.RuntimeAudioSource.Clip = component.AudioClip;
                }

                // Update 3D position if enabled
                if (component.Is3D && entity.HasComponent<TransformComponent>())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    component.RuntimeAudioSource.SetPosition(transform.Translation);

                    // Update distance properties if changed
                    component.RuntimeAudioSource.SetSpatialMode(true, component.MinDistance, component.MaxDistance);
                }

                // Sync playing state
                component.IsPlaying = component.RuntimeAudioSource.IsPlaying;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error updating audio source for entity '{entity.Name}' (ID: {entity.Id})");
            }
        }
    }
}
