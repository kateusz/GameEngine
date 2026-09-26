using System.Numerics;
using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

public class ModelLightConversionTests
{
    [Fact]
    public void TryPoint_TorchBrightness_IsUnitIntensityAndSixMeters()
    {
        var diffuse = new Vector3(4f, 2f, 1f);
        ModelLightConversion.TryPoint(diffuse, null, out var color, out var intensity, out var range).ShouldBeTrue();
        intensity.ShouldBe(1f);
        range.ShouldBe(6f);
        color.ShouldBe(new Vector4(1f, 0.5f, 0.25f, 1f));
    }

    [Fact]
    public void TryPoint_CandleBrightness_ClampsIntensityAndDerivesRange()
    {
        ModelLightConversion.TryPoint(new Vector3(0.5f, 0.2f, 0.1f), null, out _, out var intensity, out var range)
            .ShouldBeTrue();
        intensity.ShouldBe(0.5f);
        range.ShouldBe(3f * MathF.Sqrt(0.5f));
    }

    [Fact]
    public void TryPoint_FireBrightness_HitsBothCaps()
    {
        ModelLightConversion.TryPoint(new Vector3(100f, 40f, 10f), null, out _, out var intensity, out var range)
            .ShouldBeTrue();
        intensity.ShouldBe(2f);
        range.ShouldBe(16f);
    }

    [Fact]
    public void TryPoint_FileRange_ReplacesDerivedRange()
    {
        ModelLightConversion.TryPoint(new Vector3(4f, 4f, 4f), 40f, out _, out var intensity, out var range)
            .ShouldBeTrue();
        intensity.ShouldBe(1f);
        range.ShouldBe(40f);
    }

    [Fact]
    public void TryPoint_NonPositiveFileRange_UsesDerivedRange()
    {
        ModelLightConversion.TryPoint(new Vector3(4f, 4f, 4f), 0f, out _, out _, out var range).ShouldBeTrue();
        range.ShouldBe(6f);
    }

    [Fact]
    public void TryPoint_ZeroBrightness_ReturnsFalse()
    {
        ModelLightConversion.TryPoint(Vector3.Zero, 10f, out _, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryDirectional_LuxAboveOne_ScalesBrightestChannelToOne()
    {
        ModelLightConversion.TryDirectional(new Vector3(2f, 1f, 0f), new Vector3(0f, 0f, -1f), out var color, out var direction)
            .ShouldBeTrue();
        color.ShouldBe(new Vector4(1f, 0.5f, 0f, 1f));
        direction.ShouldBe(new Vector3(0f, 0f, -1f));
    }

    [Fact]
    public void TryDirectional_ColorAtMostOne_StaysUnscaled()
    {
        ModelLightConversion.TryDirectional(new Vector3(0.2f, 0.1f, 0f), new Vector3(0f, -1f, 0f), out var color, out _)
            .ShouldBeTrue();
        color.ShouldBe(new Vector4(0.2f, 0.1f, 0f, 1f));
    }

    [Fact]
    public void TryDirectional_ZeroDirection_UsesDefault()
    {
        ModelLightConversion.TryDirectional(Vector3.One, Vector3.Zero, out _, out var direction).ShouldBeTrue();
        direction.ShouldBe(new Vector3(0f, -1f, 0f));
    }
}
