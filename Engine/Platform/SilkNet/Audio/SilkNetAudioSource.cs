using System.Numerics;
using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

public class SilkNetAudioSource : IAudioSource
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SilkNetAudioSource>();

    private readonly AL _al;
    private uint _sourceId;
    private IAudioClip _clip;
    private bool _disposed = false;

    public SilkNetAudioSource(AL al)
    {
        _al = al;
        _sourceId = _al.GenSource();

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
            if (_clip != value)
            {
                Stop();
                _clip = value;

                if (_clip is SilkNetAudioClip silkClip && silkClip.IsLoaded)
                {
                    _al.SetSourceProperty(_sourceId, SourceInteger.Buffer, (int)silkClip.BufferId);
                }
            }
        }
    }

    public float Volume
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.Gain, out float volume);
            return volume;
        }
        set => _al.SetSourceProperty(_sourceId, SourceFloat.Gain, System.Math.Max(0.0f, value));
    }

    public float Pitch
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.Pitch, out float pitch);
            return pitch;
        }
        set => _al.SetSourceProperty(_sourceId, SourceFloat.Pitch, System.Math.Max(0.1f, value));
    }

    public bool Loop
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceBoolean.Looping, out bool loop);
            return loop;
        }
        set => _al.SetSourceProperty(_sourceId, SourceBoolean.Looping, value);
    }

    public bool IsPlaying
    {
        get
        {
            _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out int state);
            return state == (int)SourceState.Playing;
        }
    }

    public bool IsPaused
    {
        get
        {
            _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out int state);
            return state == (int)SourceState.Paused;
        }
    }

    public float PlaybackPosition
    {
        get
        {
            _al.GetSourceProperty(_sourceId, SourceFloat.SecOffset, out float position);
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

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();

            if (_sourceId != 0)
            {
                _al.DeleteSource(_sourceId);
                _sourceId = 0;
            }

            // Unregister from engine
            if (AudioEngine.Instance is SilkNetAudioEngine silkEngine)
            {
                silkEngine.UnregisterSource(this);
            }

            _disposed = true;
        }
    }

    ~SilkNetAudioSource()
    {
        if (!_disposed)
        {
            Logger.Warning("Warning: SilkNetAudioSource was not properly disposed. Call Dispose().");
        }
    }
}