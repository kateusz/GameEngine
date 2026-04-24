namespace Engine.Renderer;

public readonly record struct BloomSettings(
    bool Enabled,
    float Threshold,
    float SoftKnee,
    float Intensity,
    int BlurPasses,
    int DownsampleFactor,
    float Exposure,
    float Gamma)
{
    public static BloomSettings Default => new(
        Enabled: true,
        Threshold: 1.0f,
        SoftKnee: 0.5f,
        Intensity: 0.9f,
        BlurPasses: 6,
        DownsampleFactor: 2,
        Exposure: 1.0f,
        Gamma: 2.2f);
}
