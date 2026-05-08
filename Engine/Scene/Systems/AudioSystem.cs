using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Audio;
using Engine.Scene.Serializer;
using Math;
using SceneComponents;
using SceneComponents.Audio;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for managing audio playback and 3D spatial audio.
/// Handles audio source lifecycle, updates 3D positions, and manages the audio listener.
/// </summary>
internal sealed class AudioSystem(
    IAudioEngine audioEngine,
    IAudioEffectFactory effectFactory,
    IContext context) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<AudioSystem>();
    private readonly Dictionary<int, AudioRuntimeState> _runtimeByEntityId = [];

    public int Priority => SystemPriorities.AudioSystem;

    /// <summary>
    /// Initializes the audio system.
    /// Creates runtime audio sources for all entities with AudioSourceComponent.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("AudioSystem initialized with priority {Priority}", Priority);

        // Create audio sources for all entities that have AudioSourceComponent
        var view = context.View<AudioSourceComponent>();
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
        foreach (var runtimeState in _runtimeByEntityId.Values)
        {
            runtimeState.Source.Dispose();
        }
        _runtimeByEntityId.Clear();

        Logger.Debug("AudioSystem shut down");
    }

    /// <summary>
    /// Plays the audio source for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public void Play(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning("Cannot play audio for entity '{EntityName}' - no AudioSourceComponent found", entity.Name);
            return;
        }

        var component = entity.GetComponent<AudioSourceComponent>();
        var runtimeState = EnsureRuntimeState(entity);
        if (TrySyncClip(component, runtimeState, entity) && runtimeState.Source.Clip != null)
        {
            runtimeState.Source.Play();
            runtimeState.IsPlaying = true;
        }
        else
        {
            Logger.Warning("Cannot play audio for entity '{EntityName}' - no AudioClip assigned", entity.Name);
        }
    }

    /// <summary>
    /// Pauses the audio playback for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public void Pause(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning("Cannot pause audio for entity '{EntityName}' - no AudioSourceComponent found", entity.Name);
            return;
        }

        if (_runtimeByEntityId.TryGetValue(entity.Id, out var runtimeState))
        {
            runtimeState.Source.Pause();
            runtimeState.IsPlaying = false;
        }
    }

    /// <summary>
    /// Stops the audio playback for the specified entity.
    /// </summary>
    /// <param name="entity">Entity with an AudioSourceComponent.</param>
    public void Stop(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning("Cannot stop audio for entity '{EntityName}' - no AudioSourceComponent found", entity.Name);
            return;
        }

        if (_runtimeByEntityId.TryGetValue(entity.Id, out var runtimeState))
        {
            runtimeState.Source.Stop();
            runtimeState.IsPlaying = false;
        }
    }

    /// <summary>
    /// Initializes an audio source for an entity.
    /// Creates the runtime audio source and sets up initial properties.
    /// </summary>
    private void InitializeAudioSource(Entity entity, AudioSourceComponent component)
    {
        var runtimeState = EnsureRuntimeState(entity);
        try
        {
            runtimeState.Source.Volume = component.Volume;
            runtimeState.Source.Pitch = component.Pitch;
            runtimeState.Source.Loop = component.Loop;
            runtimeState.Source.SetSpatialMode(component.Is3D, component.MinDistance, component.MaxDistance);

            TrySyncClip(component, runtimeState, entity);

            if (component.Is3D && entity.HasComponent<TransformComponent>())
            {
                var transform = entity.GetComponent<TransformComponent>();
                runtimeState.Source.SetPosition(transform.Translation);
            }

            if (component.PlayOnAwake && runtimeState.Source.Clip != null)
            {
                runtimeState.Source.Play();
                runtimeState.IsPlaying = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize audio source for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
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

        var listenerView = context.View<AudioListenerComponent>();
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
        if (activeListenerEntity == null)
            return;

        var transform = activeListenerEntity.GetComponent<TransformComponent>();
        var pos = transform.Translation;

        // Set listener position
        audioEngine.SetListenerPosition(pos);

        // Set listener orientation based on transform rotation
        var quaternion = MathHelpers.QuaternionFromEuler(transform.Rotation);
        var forward = Vector3.Transform(-Vector3.UnitZ, quaternion);
        var up = Vector3.Transform(Vector3.UnitY, quaternion);

        audioEngine.SetListenerOrientation(forward, up);
    }

    /// <summary>
    /// Updates all audio sources, synchronizing their properties and 3D positions.
    /// </summary>
    private void UpdateAudioSources()
    {
        var activeEntityIds = new HashSet<int>();
        var view = context.View<AudioSourceComponent>();
        foreach (var (entity, component) in view)
        {
            activeEntityIds.Add(entity.Id);
            try
            {
                var runtimeState = EnsureRuntimeState(entity);
                runtimeState.Source.Volume = component.Volume;
                runtimeState.Source.Pitch = component.Pitch;
                runtimeState.Source.Loop = component.Loop;
                runtimeState.Source.SetSpatialMode(component.Is3D, component.MinDistance, component.MaxDistance);

                TrySyncClip(component, runtimeState, entity);

                if (component.Is3D && entity.HasComponent<TransformComponent>())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    runtimeState.Source.SetPosition(transform.Translation);
                }

                runtimeState.IsPlaying = runtimeState.Source.IsPlaying;
                SyncEffects(runtimeState.Source, component);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating audio source for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
            }
        }

        CleanupOrphanedRuntime(activeEntityIds);
    }

    private bool TrySyncClip(AudioSourceComponent component, AudioRuntimeState runtimeState, Entity entity)
    {
        var clipPath = component.AudioClipPath;
        if (string.IsNullOrWhiteSpace(clipPath))
        {
            runtimeState.Clip = null;
            runtimeState.LoadedClipPath = null;
            return false;
        }

        if (runtimeState.LoadedClipPath == clipPath && runtimeState.Clip != null)
            return true;

        try
        {
            var fullPath = PathBuilder.Build(clipPath);
            var clip = audioEngine.LoadAudioClip(fullPath);
            runtimeState.Clip = clip;
            runtimeState.LoadedClipPath = clipPath;
            runtimeState.Source.Clip = clip;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex,
                "Failed to load audio clip '{AudioClipPath}' for entity '{EntityName}'. Audio source will continue without clip.",
                clipPath, entity.Name);
            runtimeState.Clip = null;
            runtimeState.LoadedClipPath = null;
            return false;
        }
    }

    private void SyncEffects(IAudioSource source, AudioSourceComponent component)
    {
        var desiredEffects = component.Effects
            .Where(e => e.Enabled)
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Last());
        
        var typesToRemove = source.GetActiveEffectTypes().ToList();
        foreach (var type in typesToRemove)
        {
            if (!desiredEffects.ContainsKey(type))
                source.RemoveEffect(type);
        }

        // Add/update effects from config
        foreach (var config in desiredEffects.Values)
        {
            if (!source.HasEffect(config.Type))
            {
                var effect = effectFactory.CreateEffect(config.Type);
                source.AddEffect(effect);
            }
            source.UpdateEffect(config.Type, config.Amount);
        }
    }

    private AudioRuntimeState EnsureRuntimeState(Entity entity)
    {
        if (_runtimeByEntityId.TryGetValue(entity.Id, out var runtimeState))
            return runtimeState;

        runtimeState = new AudioRuntimeState(audioEngine.CreateAudioSource());
        _runtimeByEntityId[entity.Id] = runtimeState;
        return runtimeState;
    }

    private void CleanupOrphanedRuntime(HashSet<int> activeEntityIds)
    {
        var staleEntityIds = _runtimeByEntityId.Keys.Where(id => !activeEntityIds.Contains(id)).ToList();
        foreach (var staleEntityId in staleEntityIds)
        {
            _runtimeByEntityId[staleEntityId].Source.Dispose();
            _runtimeByEntityId.Remove(staleEntityId);
        }
    }

    private sealed class AudioRuntimeState(IAudioSource source)
    {
        public IAudioSource Source { get; } = source;
        public IAudioClip? Clip { get; set; }
        public string? LoadedClipPath { get; set; }
        public bool IsPlaying { get; set; }
    }
}
