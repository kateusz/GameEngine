using System.Numerics;

namespace Audio;

public interface IAudioSource : IDisposable
{
    void Play();
    void Pause();
    void Stop();

    IAudioClip Clip { get; set; }
    float Volume { get; set; }
    float Pitch { get; set; }
    bool Loop { get; set; }

    bool IsPlaying { get; }
    bool IsPaused { get; }
    float PlaybackPosition { get; set; }

    void SetPosition(Vector3 position);
    void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f);

    void AddEffect(AudioEffectType type, float amount = 0.5f);
    void RemoveEffect(AudioEffectType type);
    void ClearEffects();
    bool HasEffect(AudioEffectType type);
    void UpdateEffect(AudioEffectType type, float amount);
    IEnumerable<AudioEffectType> GetActiveEffectTypes();
}
