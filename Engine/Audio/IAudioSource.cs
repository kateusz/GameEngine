using System.Numerics;

namespace Engine.Audio;

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

    // Spatial audio - 3D positioning
    /// <summary>
    /// Sets the 3D position of the audio source in world space.
    /// Only affects sources configured for 3D audio.
    /// </summary>
    void SetPosition(Vector3 position);

    /// <summary>
    /// Configures the audio source for 3D spatial audio or 2D playback.
    /// </summary>
    /// <param name="is3D">True for 3D spatial audio, false for 2D (relative to listener)</param>
    /// <param name="minDistance">Distance at which sound starts to attenuate (3D only)</param>
    /// <param name="maxDistance">Maximum distance for sound audibility (3D only)</param>
    void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f);
}