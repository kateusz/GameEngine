namespace Engine.Platform.OpenAL.Effects;

internal static class AudioEffectConstants
{
    // Reverb decay time range (seconds)
    internal const float ReverbMinDecayTime = 0.1f;
    internal const float ReverbMaxDecayTime = 4.0f;

    // Reverb diffusion: base value + amount * range gives 0.5 to 1.0
    internal const float ReverbBaseDiffusion = 0.5f;
    internal const float ReverbDiffusionRange = 0.5f;

    // Reverb gain: base value + amount * range gives 0.32 to 1.0
    internal const float ReverbBaseGain = 0.32f;
    internal const float ReverbGainRange = 0.68f;
}
