using ECS;

namespace SceneComponents.Audio;

/// <summary>
/// Component that represents an audio source in the scene.
/// Can play audio clips with spatial 3D audio support.
/// </summary>
public class AudioSourceComponent : IComponent
{
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

    /// <summary>Audio effects applied to this source.</summary>
    public List<AudioEffectData> Effects { get; set; } = [];

    public AudioSourceComponent()
    {
    }

    public IComponent Clone()
    {
        return new AudioSourceComponent
        {
            AudioClipPath = AudioClipPath,
            Volume = Volume,
            Pitch = Pitch,
            Loop = Loop,
            PlayOnAwake = PlayOnAwake,
            Is3D = Is3D,
            MinDistance = MinDistance,
            MaxDistance = MaxDistance,
            Effects = Effects.Select(e => new AudioEffectData
            {
                Type = e.Type,
                Enabled = e.Enabled,
                Amount = e.Amount
            }).ToList()
        };
    }
}
