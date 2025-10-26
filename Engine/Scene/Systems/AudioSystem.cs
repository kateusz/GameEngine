using System.Numerics;
using ECS;
using Engine.Platform.SilkNet.Audio;
using Engine.Scene.Components;
using Serilog;
using Silk.NET.OpenAL;
using Context = ECS.Context;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for managing audio playback and 3D spatial audio.
/// Handles audio source lifecycle, updates 3D positions, and manages the audio listener.
/// </summary>
public class AudioSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<AudioSystem>();

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
            component.RuntimeAudioSource = AudioEngine.Instance.CreateAudioSource();

            // Set initial properties
            if (component.AudioClip != null)
            {
                component.RuntimeAudioSource.Clip = component.AudioClip;
            }

            component.RuntimeAudioSource.Volume = component.Volume;
            component.RuntimeAudioSource.Pitch = component.Pitch;
            component.RuntimeAudioSource.Loop = component.Loop;

            // Set 3D spatial properties if supported
            if (component.Is3D && AudioEngine.Instance is SilkNetAudioEngine silkEngine)
            {
                var al = silkEngine.GetAL();
                var source = component.RuntimeAudioSource as SilkNetAudioSource;
                if (source != null)
                {
                    var sourceId = GetSourceId(source);
                    if (sourceId != 0)
                    {
                        // Enable 3D positioning
                        al.SetSourceProperty(sourceId, SourceBoolean.SourceRelative, false);
                        al.SetSourceProperty(sourceId, SourceFloat.ReferenceDistance, component.MinDistance);
                        al.SetSourceProperty(sourceId, SourceFloat.MaxDistance, component.MaxDistance);
                        al.SetSourceProperty(sourceId, SourceFloat.RolloffFactor, 1.0f);

                        // Set initial position from transform
                        if (entity.HasComponent<TransformComponent>())
                        {
                            var transform = entity.GetComponent<TransformComponent>();
                            var pos = transform.Translation;
                            al.SetSourceProperty(sourceId, SourceVector3.Position, pos.X, pos.Y, pos.Z);
                        }
                    }
                }
            }
            else if (!component.Is3D && AudioEngine.Instance is SilkNetAudioEngine silkEngine2)
            {
                // Set as 2D audio (relative to listener)
                var al = silkEngine2.GetAL();
                var source = component.RuntimeAudioSource as SilkNetAudioSource;
                if (source != null)
                {
                    var sourceId = GetSourceId(source);
                    if (sourceId != 0)
                    {
                        al.SetSourceProperty(sourceId, SourceBoolean.SourceRelative, true);
                        al.SetSourceProperty(sourceId, SourceVector3.Position, 0.0f, 0.0f, 0.0f);
                    }
                }
            }

            // Play on awake if requested
            if (component is { PlayOnAwake: true, AudioClip: not null })
            {
                component.Play();
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
        if (AudioEngine.Instance is not SilkNetAudioEngine silkEngine)
            return;

        var al = silkEngine.GetAL();

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
        al.SetListenerProperty(ListenerVector3.Position, pos.X, pos.Y, pos.Z);

        // Set listener orientation based on transform rotation
        var transformMatrix = transform.GetTransform();
        var forward = Vector3.Transform(-Vector3.UnitZ, Quaternion.CreateFromRotationMatrix(transformMatrix));
        var up = Vector3.Transform(Vector3.UnitY, Quaternion.CreateFromRotationMatrix(transformMatrix));

        // Stack allocation is now outside the loop - only allocated once per method call
        unsafe
        {
            var orientation = stackalloc float[6];
            orientation[0] = forward.X;
            orientation[1] = forward.Y;
            orientation[2] = forward.Z;
            orientation[3] = up.X;
            orientation[4] = up.Y;
            orientation[5] = up.Z;

            al.SetListenerProperty(ListenerFloatArray.Orientation, orientation);
        }
    }

    /// <summary>
    /// Updates all audio sources, synchronizing their properties and 3D positions.
    /// </summary>
    private void UpdateAudioSources()
    {
        if (AudioEngine.Instance is not SilkNetAudioEngine silkEngine)
            return;

        var al = silkEngine.GetAL();

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
                    var pos = transform.Translation;

                    var source = component.RuntimeAudioSource as SilkNetAudioSource;
                    if (source != null)
                    {
                        var sourceId = GetSourceId(source);
                        if (sourceId != 0)
                        {
                            al.SetSourceProperty(sourceId, SourceVector3.Position, pos.X, pos.Y, pos.Z);

                            // Update distance properties if changed
                            al.SetSourceProperty(sourceId, SourceFloat.ReferenceDistance, component.MinDistance);
                            al.SetSourceProperty(sourceId, SourceFloat.MaxDistance, component.MaxDistance);
                        }
                    }
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

    /// <summary>
    /// Gets the OpenAL source ID from a SilkNetAudioSource using reflection.
    /// This is a workaround since the source ID is private.
    /// </summary>
    private static uint GetSourceId(SilkNetAudioSource source)
    {
        var field = typeof(SilkNetAudioSource).GetField("_sourceId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var value = field.GetValue(source);
            if (value is uint sourceId)
            {
                return sourceId;
            }
        }
        return 0;
    }
}
