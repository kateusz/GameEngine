using System.Numerics;
using Audio;
using SceneComponents.Audio;

namespace Engine.Platform.OpenAL;

internal sealed class NoOpAudioSource : IAudioSource
{
    private static readonly NoOpAudioClip DummyClip = new();

    public IAudioClip Clip { get; set; } = DummyClip;
    public float Volume { get; set; }
    public float Pitch { get; set; }
    public bool Loop { get; set; }
    public bool IsPlaying => false;
    public bool IsPaused => false;
    public float PlaybackPosition { get; set; }

    public void Play() { }
    public void Pause() { }
    public void Stop() { }
    public void SetPosition(Vector3 position) { }
    public void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f) { }
    public void AddEffect(AudioEffectType type, float amount = 0.5f) { }
    public void RemoveEffect(AudioEffectType type) { }
    public void ClearEffects() { }
    public bool HasEffect(AudioEffectType type) => false;
    public void UpdateEffect(AudioEffectType type, float amount) { }
    public IEnumerable<AudioEffectType> GetActiveEffectTypes() => [];
    public void Dispose() { }
}