using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Math;
using SceneComponents;
using SceneComponents.Audio;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class AudioSystem(
    IAudio audio,
    IContext context,
    AudioPlaybackService playbackService) : ISystem, IAudioPlayback
{
    private static readonly ILogger Logger = Log.ForContext<AudioSystem>();
    private readonly Dictionary<int, AudioRuntimeState> _runtimeByEntityId = [];

    public int Priority => SystemPriorities.AudioSystem;

    public void OnInit()
    {
        Logger.Debug("AudioSystem initialized with priority {Priority}", Priority);

        foreach (var (entity, component) in context.View<AudioSourceComponent>())
            InitializeAudioSource(entity, component);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        UpdateListener();
        UpdateAudioSources();
    }

    public void OnShutdown()
    {
        playbackService.Unbind(this);

        foreach (var runtimeState in _runtimeByEntityId.Values)
            runtimeState.Source.Dispose();

        _runtimeByEntityId.Clear();
        audio.ClearClipCache();

        Logger.Debug("AudioSystem shut down");
    }

    public void Play(Entity entity)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
        {
            Logger.Warning("Cannot play audio for entity '{EntityName}' - no AudioSourceComponent found", entity.Name);
            return;
        }

        var component = entity.GetComponent<AudioSourceComponent>();
        var runtimeState = EnsureRuntimeState(entity);

        try
        {
            ApplyComponentToSource(entity, component, runtimeState, force: true);

            if (runtimeState.Source.Clip != null)
            {
                runtimeState.Source.Play();
                runtimeState.IsPlaying = true;
            }
            else
            {
                Logger.Warning("Cannot play audio for entity '{EntityName}' - no AudioClip assigned", entity.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to play audio for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
        }
    }

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

    private void InitializeAudioSource(Entity entity, AudioSourceComponent component)
    {
        var runtimeState = EnsureRuntimeState(entity);
        try
        {
            ApplyComponentToSource(entity, component, runtimeState, force: true);

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

    private void UpdateListener()
    {
        foreach (var (_, component, transform) in context.View<AudioListenerComponent, TransformComponent>())
        {
            if (!component.IsActive)
                continue;

            var pos = transform.GetWorldTransform().Translation;
            audio.SetListenerPosition(pos);

            var quaternion = MathHelpers.QuaternionFromEuler(transform.Rotation);
            var forward = Vector3.Transform(-Vector3.UnitZ, quaternion);
            var up = Vector3.Transform(Vector3.UnitY, quaternion);

            audio.SetListenerOrientation(forward, up);
            return;
        }
    }

    private void UpdateAudioSources()
    {
        var activeEntityIds = new HashSet<int>();
        foreach (var (entity, component) in context.View<AudioSourceComponent>())
        {
            activeEntityIds.Add(entity.Id);
            try
            {
                var runtimeState = EnsureRuntimeState(entity);
                ApplyComponentToSource(entity, component, runtimeState);
                runtimeState.IsPlaying = runtimeState.Source.IsPlaying;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating audio source for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
            }
        }

        CleanupOrphanedRuntime(activeEntityIds);
    }

    private void ApplyComponentToSource(
        Entity entity,
        AudioSourceComponent component,
        AudioRuntimeState runtimeState,
        bool force = false)
    {
        if (force || runtimeState.LastVolume != component.Volume)
        {
            runtimeState.Source.Volume = component.Volume;
            runtimeState.LastVolume = component.Volume;
        }

        if (force || runtimeState.LastPitch != component.Pitch)
        {
            runtimeState.Source.Pitch = component.Pitch;
            runtimeState.LastPitch = component.Pitch;
        }

        if (force || runtimeState.LastLoop != component.Loop)
        {
            runtimeState.Source.Loop = component.Loop;
            runtimeState.LastLoop = component.Loop;
        }

        if (force
            || runtimeState.LastIs3D != component.Is3D
            || runtimeState.LastMinDistance != component.MinDistance
            || runtimeState.LastMaxDistance != component.MaxDistance)
        {
            runtimeState.Source.SetSpatialMode(component.Is3D, component.MinDistance, component.MaxDistance);
            runtimeState.LastIs3D = component.Is3D;
            runtimeState.LastMinDistance = component.MinDistance;
            runtimeState.LastMaxDistance = component.MaxDistance;
        }

        TrySyncClip(component, runtimeState, entity);

        if (component.Is3D && entity.TryGetComponent<TransformComponent>(out var transform))
            runtimeState.Source.SetPosition(transform.GetWorldTransform().Translation);

        var effectsHash = ComputeEffectsHash(component.Effects);
        if (force || effectsHash != runtimeState.LastEffectsHash)
        {
            runtimeState.LastEffectsHash = effectsHash;
            SyncEffects(runtimeState.Source, component);
        }
    }

    private static int ComputeEffectsHash(List<AudioEffectData> effects)
    {
        var hash = new HashCode();
        foreach (var effect in effects)
            hash.Add(HashCode.Combine(effect.Type, effect.Enabled, effect.Amount));

        return hash.ToHashCode();
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
            var clip = audio.LoadAudioClip(fullPath);
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

    private static void SyncEffects(IAudioSource source, AudioSourceComponent component)
    {
        var desiredEffects = new Dictionary<AudioEffectType, AudioEffectData>();
        foreach (var effect in component.Effects)
        {
            if (effect.Enabled)
                desiredEffects[effect.Type] = effect;
        }

        foreach (var type in source.GetActiveEffectTypes().ToList())
        {
            if (!desiredEffects.ContainsKey(type))
                source.RemoveEffect(type);
        }

        foreach (var config in desiredEffects.Values)
        {
            if (!source.HasEffect(config.Type))
                source.AddEffect(config.Type, config.Amount);
            else
                source.UpdateEffect(config.Type, config.Amount);
        }
    }

    private AudioRuntimeState EnsureRuntimeState(Entity entity)
    {
        if (_runtimeByEntityId.TryGetValue(entity.Id, out var runtimeState))
            return runtimeState;

        runtimeState = new AudioRuntimeState(audio.CreateAudioSource());
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
        public float LastVolume = float.NaN;
        public float LastPitch = float.NaN;
        public bool LastLoop;
        public bool LastIs3D;
        public float LastMinDistance = float.NaN;
        public float LastMaxDistance = float.NaN;
        public int LastEffectsHash;
    }
}
