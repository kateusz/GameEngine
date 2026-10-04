namespace Engine.Renderer.Pipeline;

public enum ShadowPreset
{
    Low,
    Medium,
    High
}

public readonly record struct ShadowQuality(
    int DirectionalResolution = 1024,
    int PointResolution = 512,
    float Distance = 50f,
    int PcfTaps = 1,
    bool PointPcf = false,
    int Cascades = 1)
{
    public ShadowQuality Sanitized()
    {
        var sun = DirectionalResolution switch
        {
            >= 4096 => 4096,
            >= 2048 => 2048,
            _ => 1024
        };
        var point = PointResolution switch
        {
            >= 1024 => 1024,
            >= 512 => 512,
            _ => 256
        };
        var taps = PcfTaps >= 5 ? 5 : PcfTaps >= 3 ? 3 : 1;
        var distance = float.IsFinite(Distance) ? System.Math.Clamp(Distance, 1f, 200f) : 50f;
        var cascades = Cascades >= 2 ? 2 : 1;
        return new ShadowQuality(sun, point, distance, taps, PointPcf, cascades);
    }

    public static ShadowQuality For(ShadowPreset preset) => preset switch
    {
        ShadowPreset.High => new(2048, 1024, 50f, PcfTaps: 5, PointPcf: true, Cascades: 2),
        ShadowPreset.Medium => new(2048, 512, 50f, PcfTaps: 3, Cascades: 2),
        _ => new ShadowQuality()
    };

    public static ShadowPreset? Matching(ShadowQuality quality)
    {
        var sanitized = quality.Sanitized();
        if (sanitized == For(ShadowPreset.High))
            return ShadowPreset.High;
        if (sanitized == For(ShadowPreset.Medium))
            return ShadowPreset.Medium;
        if (sanitized == For(ShadowPreset.Low))
            return ShadowPreset.Low;
        return null;
    }
}
