using System.Numerics;

namespace Engine.Audio;

public interface IAudioEngine
{
    void Initialize();
    void Shutdown();
    IAudioSource CreateAudioSource();
    IAudioClip LoadAudioClip(string path);
    void UnloadAudioClip(string path);
    void PlayOneShot(string clipPath, float volume = 1.0f);

    // Listener management methods
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
}