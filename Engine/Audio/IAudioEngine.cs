using System.Numerics;

namespace Engine.Audio;

public interface IAudioEngine : IDisposable
{
    void Initialize();
    IAudioSource CreateAudioSource();
    IAudioClip LoadAudioClip(string path);
    void UnloadAudioClip(string path);
    void PlayOneShot(string clipPath, float volume = 1.0f);
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
}