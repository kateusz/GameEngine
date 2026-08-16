namespace Engine.Renderer;

public readonly record struct PostProcessSettings(
    float Exposure = 1.8f,
    bool BloomEnabled = false,
    float BloomThreshold = 1f,
    float BloomIntensity = 0f,
    bool FxaaEnabled = false);
