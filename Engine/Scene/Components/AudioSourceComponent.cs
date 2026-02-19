using System.Text.Json.Serialization;
using ECS;
using Engine.Audio;

namespace Engine.Scene.Components;

/// <summary>
/// Component that represents an audio source in the scene.
/// Can play audio clips with spatial 3D audio support.
/// </summary>
public class AudioSourceComponent : IComponent
{
    private IAudioClip? _audioClip;

    /// <summary>
    /// The audio clip to play.
    /// </summary>
    [JsonIgnore]
    public IAudioClip? AudioClip
    {
        get => _audioClip;
        set
        {
            _audioClip = value;
            AudioClipPath = value?.Path;
        }
    }

    public string? AudioClipPath { get; set; }

    /// <summary>
    /// Volume of the audio source (0.0 to 1.0).
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Pitch of the audio source (0.5 to 2.0 typical range).
    /// </summary>
    public float Pitch { get; set; } = 1.0f;

    /// <summary>
    /// Whether the audio should loop.
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Whether the audio should play automatically when the scene starts.
    /// </summary>
    public bool PlayOnAwake { get; set; } = false;

    /// <summary>
    /// Whether this is a 3D spatial audio source.
    /// If false, the audio will be played as 2D (no spatial positioning).
    /// </summary>
    public bool Is3D { get; set; } = true;

    /// <summary>
    /// Minimum distance for 3D audio attenuation.
    /// Within this distance, audio is at full volume.
    /// </summary>
    public float MinDistance { get; set; } = 1.0f;

    /// <summary>
    /// Maximum distance for 3D audio attenuation.
    /// Beyond this distance, audio volume is significantly reduced.
    /// </summary>
    public float MaxDistance { get; set; } = 100.0f;

    /// <summary>
    /// Whether the audio source is currently playing.
    /// </summary>
    public bool IsPlaying { get; set; } = false;

    /// <summary>
    /// Internal reference to the OpenAL audio source.
    /// Managed by the AudioSystem.
    /// </summary>
    [JsonIgnore]
    internal IAudioSource? RuntimeAudioSource { get; set; }

    public AudioSourceComponent()
    {
    }

    public AudioSourceComponent(IAudioClip? audioClip, float volume = 1.0f, float pitch = 1.0f,
        bool loop = false, bool playOnAwake = false, bool is3D = true,
        float minDistance = 1.0f, float maxDistance = 100.0f)
    {
        AudioClip = audioClip;
        AudioClipPath = audioClip?.Path;
        Volume = volume;
        Pitch = pitch;
        Loop = loop;
        PlayOnAwake = playOnAwake;
        Is3D = is3D;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
    }

    public IComponent Clone()
    {
        return new AudioSourceComponent(AudioClip, Volume, Pitch, Loop, PlayOnAwake, Is3D, MinDistance, MaxDistance);
    }
}
