namespace Engine.Audio.Effects;

/// <summary>
/// Represents an effect slot that can hold an audio effect.
/// Effect slots are limited resources in OpenAL - typical implementations support 2-4 per source.
/// </summary>
public interface IAudioEffectSlot : IDisposable
{
    /// <summary>
    /// Gets the OpenAL effect slot ID.
    /// </summary>
    uint SlotId { get; }

    /// <summary>
    /// Gets or sets the effect attached to this slot.
    /// Can be null if no effect is attached.
    /// </summary>
    IAudioEffect? Effect { get; set; }

    /// <summary>
    /// Gets or sets the overall gain/volume of the effect (0.0 - 1.0).
    /// Controls how much of the effect is heard.
    /// </summary>
    float Gain { get; set; }

    /// <summary>
    /// Gets whether this effect slot is currently active.
    /// </summary>
    bool IsActive { get; }
}
