using System.Numerics;
using Engine.Platform.SilkNet;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class EnvironmentBakeTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Bake_TinyWarmSunOverBlueSky_IrradianceStaysWarm()
    {
        var path = Path.Combine(Path.GetTempPath(), $"envsun-{Guid.NewGuid():N}.hdr");
        try
        {
            HdrTestImages.WriteBlueSkyWithWarmSun(path, 32, 16);

            var map = fixture.EnvironmentMapFactory.GetOrCreate(path);
            map.ShouldNotBeNull();

            var plusY = MeanRgb(ReadCubeFace(map!.Irradiance.GetRendererId(), TextureTarget.TextureCubeMapPositiveY, 32));
            var minusY = MeanRgb(ReadCubeFace(map.Irradiance.GetRendererId(), TextureTarget.TextureCubeMapNegativeY, 32));
            var warmest = plusY.R >= minusY.R ? plusY : minusY;

            // Clamping the sun texel to ~20 leaves only the blue sky (B > R). Unclamped, the
            // sun dominates the cosine integral and the upward irradiance stays warm.
            warmest.R.ShouldBeGreaterThan(warmest.B);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static (float R, float G, float B) MeanRgb(float[] pixels)
    {
        double r = 0, g = 0, b = 0;
        var count = pixels.Length / 4;
        for (var i = 0; i < count; i++)
        {
            r += pixels[i * 4];
            g += pixels[i * 4 + 1];
            b += pixels[i * 4 + 2];
        }

        return ((float)(r / count), (float)(g / count), (float)(b / count));
    }

    [GraphicsFact]
    public void Bake_SolidRedSky_YieldsRedDominantIrradiance()
    {
        var path = Path.Combine(Path.GetTempPath(), $"envbake-{Guid.NewGuid():N}.hdr");
        try
        {
            HdrTestImages.WriteSolidHdr(path, 4, 2, new Vector3(1f, 0f, 0f));

            var map = fixture.EnvironmentMapFactory.GetOrCreate(path);

            map.ShouldNotBeNull();
            var pixels = ReadCubeFace(map!.Irradiance.GetRendererId(), TextureTarget.TextureCubeMapPositiveX, 32);
            var center = (32 / 2 * 32 + 32 / 2) * 4;
            pixels[center].ShouldBeGreaterThan(0.5f);
            pixels[center].ShouldBeLessThan(1.5f);
            pixels[center + 1].ShouldBeLessThan(0.02f);
            pixels[center + 2].ShouldBeLessThan(0.02f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [GraphicsFact]
    public void GetOrCreate_MissingFile_ReturnsNullAndCachesFailure()
    {
        var missing = Path.Combine(Path.GetTempPath(), "does-not-exist.hdr");
        fixture.EnvironmentMapFactory.GetOrCreate(missing).ShouldBeNull();
        fixture.EnvironmentMapFactory.GetOrCreate(missing).ShouldBeNull();
    }

    [GraphicsFact]
    public void Bake_SolidRedSky_PrefilterHighMipStaysRed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"envpre-{Guid.NewGuid():N}.hdr");
        try
        {
            HdrTestImages.WriteSolidHdr(path, 4, 2, new Vector3(1f, 0f, 0f));
            var map = fixture.EnvironmentMapFactory.GetOrCreate(path);

            map.ShouldNotBeNull();
            var pixels = ReadCubeFace(map!.Prefiltered.GetRendererId(), TextureTarget.TextureCubeMapPositiveX, 8, mip: 4);
            var center = (8 / 2 * 8 + 8 / 2) * 4;
            pixels[center].ShouldBeGreaterThan(0.5f);
            pixels[center + 1].ShouldBeLessThan(0.02f);
            float.IsNaN(pixels[center]).ShouldBeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [GraphicsFact]
    public unsafe void BrdfLut_HasSaneSplitSumValues()
    {
        var lut = fixture.EnvironmentMapFactory.GetBrdfLutId();
        lut.ShouldNotBe(0u);

        var gl = SilkNetContext.GL;
        gl.BindTexture(TextureTarget.Texture2D, lut);
        var pixels = new float[512 * 512 * 4];
        fixed (float* p = pixels)
            gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.Float, p);
        gl.BindTexture(TextureTarget.Texture2D, 0);

        var idx = (25 * 512 + 480) * 4;
        pixels[idx].ShouldBeGreaterThan(0.8f);
        pixels[idx].ShouldBeLessThan(1.2f);
        pixels[idx + 1].ShouldBeGreaterThanOrEqualTo(0f);
        pixels[idx + 1].ShouldBeLessThan(0.2f);

        var mid = (256 * 512 + 256) * 4;
        float.IsNaN(pixels[mid]).ShouldBeFalse();
        (pixels[mid] + pixels[mid + 1]).ShouldBeLessThanOrEqualTo(1.1f);
        pixels[mid].ShouldBeGreaterThan(0.1f);
    }

    internal static unsafe float[] ReadCubeFace(uint textureId, TextureTarget face, int size, int mip = 0)
    {
        var gl = SilkNetContext.GL;
        gl.BindTexture(TextureTarget.TextureCubeMap, textureId);
        var pixels = new float[size * size * 4];
        fixed (float* p = pixels)
            gl.GetTexImage(face, mip, PixelFormat.Rgba, PixelType.Float, p);
        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        return pixels;
    }
}
