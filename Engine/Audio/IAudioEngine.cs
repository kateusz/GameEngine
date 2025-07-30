namespace Engine.Audio;

public interface IAudioEngine
{
    void Initialize();
    void Shutdown();
        
    // Tworzenie obiektów
    IAudioSource CreateAudioSource();
        
    // Zarządzanie zasobami
    IAudioClip LoadAudioClip(string path);
    void UnloadAudioClip(string path);
        
    // Quick play methods
    void PlayOneShot(string clipPath, float volume = 1.0f);
        
    // Singleton access
    static IAudioEngine Instance { get; }
}