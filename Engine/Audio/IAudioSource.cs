namespace Engine.Audio;

public interface IAudioSource : IDisposable
{
    void Play();
    void Pause();
    void Stop();
        
    // Właściwości
    IAudioClip Clip { get; set; }
    float Volume { get; set; }
    float Pitch { get; set; }
    bool Loop { get; set; }
        
    // Stany
    bool IsPlaying { get; }
    bool IsPaused { get; }
    float PlaybackPosition { get; set; }
}