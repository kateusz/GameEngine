namespace Engine.Scene;

public readonly record struct ScenePostProcessSettings(
    float Exposure = 1.8f,
    bool BloomEnabled = true,
    float BloomThreshold = 1f,
    float BloomIntensity = 1f);
