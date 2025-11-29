namespace Engine.Audio;

internal static class AudioUtils
{
    public static float DecibelToLinear(float decibel)
    {
        return (float)System.Math.Pow(10.0, decibel / 20.0);
    }

    public static float LinearToDecibel(float linear)
    {
        return (float)(20.0 * System.Math.Log10(System.Math.Max(0.0001f, linear)));
    }

    public static float SemitonesToPitch(float semitones)
    {
        return (float)System.Math.Pow(2.0, semitones / 12.0);
    }

    public static float PitchToSemitones(float pitch)
    {
        return (float)(12.0 * System.Math.Log(pitch) / System.Math.Log(2.0));
    }

    public static bool IsValidVolume(float volume)
    {
        return volume is >= 0.0f and <= 1.0f;
    }

    public static bool IsValidPitch(float pitch)
    {
        return pitch is > 0.0f and <= 4.0f;
    }
}