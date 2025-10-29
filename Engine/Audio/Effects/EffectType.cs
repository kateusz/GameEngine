namespace Engine.Audio.Effects;

/// <summary>
/// Types of audio effects available via OpenAL EFX.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// No effect applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Reverb effect - simulates sound reflections in an environment.
    /// </summary>
    Reverb,

    /// <summary>
    /// EAX Reverb - enhanced reverb with more parameters.
    /// </summary>
    EAXReverb,

    /// <summary>
    /// Echo effect - repeating delay.
    /// </summary>
    Echo,

    /// <summary>
    /// Chorus effect - thickens sound by adding delayed copies with pitch variation.
    /// </summary>
    Chorus,

    /// <summary>
    /// Distortion effect - adds harmonic distortion.
    /// </summary>
    Distortion,

    /// <summary>
    /// Flanger effect - sweeping comb filter.
    /// </summary>
    Flanger,

    /// <summary>
    /// Frequency shifter - shifts audio frequency up or down.
    /// </summary>
    FrequencyShifter,

    /// <summary>
    /// Vocal morpher - morphs between phoneme sounds.
    /// </summary>
    VocalMorpher,

    /// <summary>
    /// Ring modulator - amplitude modulation effect.
    /// </summary>
    RingModulator,

    /// <summary>
    /// Autowah - dynamic wah filter.
    /// </summary>
    Autowah,

    /// <summary>
    /// Compressor - dynamic range compression.
    /// </summary>
    Compressor,

    /// <summary>
    /// Equalizer - frequency band adjustment.
    /// </summary>
    Equalizer
}
