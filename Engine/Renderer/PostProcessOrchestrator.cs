using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.Renderer;

/// <summary>
/// Coordinates HDR post passes: bloom → tonemap → optional FXAA.
/// </summary>
public sealed class PostProcessOrchestrator(
    HdrTonemapPass hdrTonemapPass,
    BloomPass bloomPass,
    FxaaPass fxaaPass)
{
    public void Initialize()
    {
        bloomPass.Initialize();
        fxaaPass.Initialize();
    }

    /// <param name="tonemapTarget">SDR destination for tonemap. Null writes to the backbuffer.</param>
    /// <param name="fxaaToBackbuffer">
    /// When FXAA is enabled and <paramref name="tonemapTarget"/> is set, blit FXAA to the backbuffer
    /// instead of an intermediate owned framebuffer.
    /// </param>
    /// <returns>Color attachment id to display in UI. 0 when the final image is on the backbuffer.</returns>
    public uint Run(
        uint hdrColorAttachmentId,
        uint width,
        uint height,
        in PostProcessSettings settings,
        IFrameBuffer? tonemapTarget,
        bool fxaaToBackbuffer = false)
    {
        uint bloomColorId = 0;
        if (settings is { BloomEnabled: true, BloomIntensity: > 0f })
        {
            bloomColorId = bloomPass.Apply(
                    hdrColorAttachmentId, width, height, settings.BloomThreshold)
                .GetColorAttachmentRendererId();
        }

        hdrTonemapPass.Apply(
            hdrColorAttachmentId,
            tonemapTarget,
            settings.Exposure,
            bloomColorId,
            settings.BloomIntensity);

        if (tonemapTarget is null)
            return 0;

        var sdrColorId = tonemapTarget.GetColorAttachmentRendererId();
        if (!settings.FxaaEnabled)
            return sdrColorId;

        if (fxaaToBackbuffer)
        {
            fxaaPass.ApplyTo(sdrColorId, null, width, height);
            return 0;
        }

        return fxaaPass.Apply(sdrColorId, width, height).GetColorAttachmentRendererId();
    }
}
