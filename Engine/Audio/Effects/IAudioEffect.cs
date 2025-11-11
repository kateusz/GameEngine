namespace Engine.Audio.Effects;

/// <summary>
/// Interface for audio effects using OpenAL EFX.
/// </summary>
public interface IAudioEffect : IDisposable
{
    /// <summary>
    /// Gets the OpenAL effect ID.
    /// </summary>
    uint EffectId { get; }

    /// <summary>
    /// Gets the type of this effect.
    /// </summary>
    EffectType Type { get; }

    /// <summary>
    /// Applies all parameters to the OpenAL effect.
    /// Call this after changing effect parameters.
    /// </summary>
    void Apply();
}
