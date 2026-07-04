using System.Numerics;

namespace Audio;

public interface IAudio : IDisposable
{
    void Initialize();
    void Update(TimeSpan deltaTime);
    IAudioSource CreateAudioSource();
    IAudioClip LoadAudioClip(string path);
    void UnloadAudioClip(string path);
    void ClearClipCache();
    void PlayOneShot(string clipPath, float volume = 1.0f);
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
}
