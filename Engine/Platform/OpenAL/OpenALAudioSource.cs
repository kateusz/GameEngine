using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Audio;
using Engine.Platform.OpenAL.Effects;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL;

internal sealed class OpenALAudioSource : IAudioSource
{
    private static readonly ILogger Logger = Log.ForContext<OpenALAudioSource>();

    // AL_AUXILIARY_SEND_FILTER requires alSource3i (slot, sendIndex, filter)
    private delegate void AlSource3iDelegate(uint source, int param, int v1, int v2, int v3);
    private const int AlAuxiliarySendFilter = 0x20006;
    private const int AlDirectFilter = 0x20005;
    private const int AlFilterNull = 0;

    private readonly AL _al;
    private readonly Action<OpenALAudioSource> _onDispose;
    private readonly AlSource3iDelegate? _source3i;
    private readonly Dictionary<AudioEffectType, IAudioEffect> _effects = new();
    private uint _sourceId;
    private IAudioClip _clip;
    private bool _disposed = false;

    public OpenALAudioSource(AL al, Action<OpenALAudioSource> onDispose)
    {
        _al = al;
        _onDispose = onDispose;
        _sourceId = _al.GenSource();

        // Load alSource3i for auxiliary send filter routing (EFX)
        var source3iPtr = al.GetProcAddress("alSource3i");
        if (source3iPtr != IntPtr.Zero)
            _source3i = Marshal.GetDelegateForFunctionPointer<AlSource3iDelegate>(source3iPtr);

        // Set default properties
        _al.SetSourceProperty(_sourceId, SourceFloat.Gain, 1.0f);
        _al.SetSourceProperty(_sourceId, SourceFloat.Pitch, 1.0f);
        _al.SetSourceProperty(_sourceId, SourceBoolean.Looping, false);
    }

    public IAudioClip Clip
    {
        get => _clip;
        set
        {
            if (_clip == value) 
                return;
            
            Stop();
            _clip = value;

            if (_clip is OpenALAudioClip { IsLoaded: true } silkClip)
            {
                _al.SetSourceProperty(_sourceId, SourceInteger.Buffer, (int)silkClip.BufferId);
            }
        }
    }

    public float Volume
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.Gain, out var volume);
            return volume;
        }
        set => _al.SetSourceProperty(_sourceId, SourceFloat.Gain, System.Math.Max(0.0f, value));
    }

    public float Pitch
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.Pitch, out var pitch);
            return pitch;
        }
        set => _al.SetSourceProperty(_sourceId, SourceFloat.Pitch, System.Math.Max(0.1f, value));
    }

    public bool Loop
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceBoolean.Looping, out var loop);
            return loop;
        }
        set => _al.SetSourceProperty(_sourceId, SourceBoolean.Looping, value);
    }

    public bool IsPlaying
    {
        get
        {
            _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out var state);
            return state == (int)SourceState.Playing;
        }
    }

    public bool IsPaused
    {
        get
        {
            _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out var state);
            return state == (int)SourceState.Paused;
        }
    }

    public float PlaybackPosition
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.SecOffset, out var position);
            return position;
        }
        set => _al.SetSourceProperty(_sourceId, SourceFloat.SecOffset, System.Math.Max(0.0f, value));
    }

    public void Play()
    {
        if (_clip == null)
        {
            Logger.Warning("Cannot play - no audio clip assigned");
            return;
        }

        if (!_clip.IsLoaded)
        {
            Logger.Warning("Cannot play - audio clip is not loaded");
            return;
        }

        _al.SourcePlay(_sourceId);
    }

    public void Pause()
    {
        _al.SourcePause(_sourceId);
    }

    public void Stop()
    {
        _al.SourceStop(_sourceId);
    }

    public void SetPosition(Vector3 position)
    {
        _al.SetSourceProperty(_sourceId, SourceVector3.Position, position.X, position.Y, position.Z);
    }

    public void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f)
    {
        if (is3D)
        {
            // Enable 3D positioning (absolute world space)
            _al.SetSourceProperty(_sourceId, SourceBoolean.SourceRelative, false);
            _al.SetSourceProperty(_sourceId, SourceFloat.ReferenceDistance, minDistance);
            _al.SetSourceProperty(_sourceId, SourceFloat.MaxDistance, maxDistance);
            _al.SetSourceProperty(_sourceId, SourceFloat.RolloffFactor, 1.0f);
        }
        else
        {
            // Set as 2D audio (relative to listener)
            _al.SetSourceProperty(_sourceId, SourceBoolean.SourceRelative, true);
            _al.SetSourceProperty(_sourceId, SourceVector3.Position, 0.0f, 0.0f, 0.0f);
        }
    }

    public void AddEffect(IAudioEffect effect)
    {
        if (_effects.ContainsKey(effect.Type))
        {
            Logger.Warning("Effect {Type} already exists on source {SourceId}", effect.Type, _sourceId);
            return;
        }

        _effects[effect.Type] = effect;
        ConnectEffect(effect);
        Logger.Debug("Added {Type} effect to source {SourceId}", effect.Type, _sourceId);
    }

    public void RemoveEffect(AudioEffectType type)
    {
        if (!_effects.Remove(type, out var effect))
            return;

        effect.Dispose();
        ReconnectEffectSlots();
        Logger.Debug("Removed {Type} effect from source {SourceId}", type, _sourceId);
    }

    public void ClearEffects()
    {
        foreach (var effect in _effects.Values)
            effect.Dispose();
        _effects.Clear();
        DisconnectAllEffectSlots();
    }

    public bool HasEffect(AudioEffectType type) => _effects.ContainsKey(type);

    public void UpdateEffect(AudioEffectType type, float amount)
    {
        if (_effects.TryGetValue(type, out var effect))
            effect.Apply(amount);
    }

    public IEnumerable<AudioEffectType> GetActiveEffectTypes() => _effects.Keys;

    private void ConnectEffect(IAudioEffect effect)
    {
        if (effect is OpenALLowPassEffect lowPass)
        {
            _al.SetSourceProperty(_sourceId, (SourceInteger)AlDirectFilter, (int)lowPass.FilterId);
            return;
        }

        if (effect.SlotId != 0 && _source3i != null)
        {
            var sendIndex = _effects.Values.Count(e => e.SlotId != 0) - 1;
            if (sendIndex < 4)
                _source3i(_sourceId, AlAuxiliarySendFilter, (int)effect.SlotId, sendIndex, AlFilterNull);
        }
    }

    private void DisconnectAllEffectSlots()
    {
        _al.SetSourceProperty(_sourceId, (SourceInteger)AlDirectFilter, AlFilterNull);

        if (_source3i == null)
            return;

        for (var i = 0; i < 4; i++)
            _source3i(_sourceId, AlAuxiliarySendFilter, AlFilterNull, i, AlFilterNull);
    }

    private void ReconnectEffectSlots()
    {
        DisconnectAllEffectSlots();

        var sendIndex = 0;
        foreach (var effect in _effects.Values)
        {
            if (effect is OpenALLowPassEffect lowPass)
            {
                _al.SetSourceProperty(_sourceId, (SourceInteger)AlDirectFilter, (int)lowPass.FilterId);
            }
            else if (effect.SlotId != 0 && sendIndex < 4 && _source3i != null)
            {
                _source3i(_sourceId, AlAuxiliarySendFilter, (int)effect.SlotId, sendIndex, AlFilterNull);
                sendIndex++;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            ClearEffects();
            Stop();

            if (_sourceId != 0)
            {
                _al.DeleteSource(_sourceId);
                Logger.Debug("Disposed AudioSource {SourceId}", _sourceId);
                _sourceId = 0;
            }

            // Notify engine that this source is being disposed
            _onDispose?.Invoke(this);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error disposing AudioSource {SourceId}", _sourceId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenALAudioSource()
    {
        if (!_disposed && _sourceId != 0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"AUDIO LEAK: AudioSource {_sourceId} not disposed!"
            );
        }
    }
#endif
}