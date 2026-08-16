using Engine.Scene;

namespace Engine.Renderer.PostProcessing;

public readonly record struct PostProcessSettings(
    float Exposure = 1.8f,
    bool BloomEnabled = false,
    float BloomThreshold = 1f,
    float BloomIntensity = 0f,
    bool FxaaEnabled = false)
{
    public static PostProcessSettings FromScene(ScenePostProcessSettings scene, bool fxaaEnabled = false) =>
        new(scene.Exposure, scene.BloomEnabled, scene.BloomThreshold, scene.BloomIntensity, fxaaEnabled);
}
