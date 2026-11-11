using Engine.Renderer.Textures.Compression;

namespace Engine.Tests.Renderer;

public class TextureBcCompressionTests
{
    [Fact]
    public void BcFormatPick_OpaqueSrgb_UsesDxt1()
    {
        var rgba = SolidRgba(4, 4, 200, 100, 50, 255);
        Assert.Equal(BcGpuFormat.Dxt1Srgb,
            BcFormatPick.Pick(sRgb: true, isNormalMap: false, isOcclusionMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_Alpha_UsesDxt5Srgb()
    {
        var rgba = SolidRgba(4, 4, 200, 100, 50, 128);
        Assert.Equal(BcGpuFormat.Dxt5Srgb,
            BcFormatPick.Pick(sRgb: true, isNormalMap: false, isOcclusionMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_Normal_UsesBc5()
    {
        var rgba = SolidRgba(4, 4, 128, 128, 255, 255);
        Assert.Equal(BcGpuFormat.Bc5, BcFormatPick.Pick(sRgb: false, isNormalMap: true, isOcclusionMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_Occlusion_UsesBc4()
    {
        var rgba = SolidRgba(4, 4, 90, 90, 90, 255);
        Assert.Equal(BcGpuFormat.Bc4,
            BcFormatPick.Pick(sRgb: false, isNormalMap: false, isOcclusionMap: true, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_GrayscaleLinear_UsesDxt5Linear()
    {
        var rgba = SolidRgba(4, 4, 90, 90, 90, 255);
        Assert.Equal(BcGpuFormat.Dxt5Linear,
            BcFormatPick.Pick(sRgb: false, isNormalMap: false, isOcclusionMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_MetallicRoughness_UsesDxt5Linear()
    {
        var rgba = new byte[4 * 4 * 4];
        for (var i = 0; i < 16; i++)
        {
            rgba[i * 4] = 10;
            rgba[i * 4 + 1] = 200;
            rgba[i * 4 + 2] = 50;
            rgba[i * 4 + 3] = 255;
        }

        Assert.Equal(BcGpuFormat.Dxt5Linear,
            BcFormatPick.Pick(sRgb: false, isNormalMap: false, isOcclusionMap: false, rgba, 4, 4));
    }

    [Fact]
    public void TextureBcSource_ChaosAlbedo_InferSrgb()
    {
        var (sRgb, isNormal) = TextureBcSource.InferEncodeRole("/assets/chaos_albedo.png");
        Assert.True(sRgb);
        Assert.False(isNormal);
    }

    [Fact]
    public void BcDdsSidecarPath_SeparatesByExtensionAndColorSpace()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bc-sidecar-" + Guid.NewGuid());
        var png = Path.Combine(dir, "wall.png");
        var jpg = Path.Combine(dir, "wall.jpg");
        Assert.NotEqual(BcDdsSidecarPath.ForSource(png, sRgb: true), BcDdsSidecarPath.ForSource(jpg, sRgb: true));
        Assert.NotEqual(BcDdsSidecarPath.ForSource(png, sRgb: true), BcDdsSidecarPath.ForSource(png, sRgb: false));
    }

    [Fact]
    public void BcDds_RoundTrip_Bc4()
    {
        RoundTripFormat(BcGpuFormat.Bc4, sRgb: false, isNormal: false);
    }

    [Fact]
    public void BcDds_RoundTrip_AllOtherFormats()
    {
        RoundTripFormat(BcGpuFormat.Dxt1Srgb, sRgb: true, isNormal: false);
        RoundTripFormat(BcGpuFormat.Dxt5Srgb, sRgb: true, isNormal: false);
        RoundTripFormat(BcGpuFormat.Dxt5Linear, sRgb: false, isNormal: false);
        RoundTripFormat(BcGpuFormat.Bc5, sRgb: false, isNormal: true);
    }

    private static void RoundTripFormat(BcGpuFormat format, bool sRgb, bool isNormal)
    {
        var rgba = SolidRgba(4, 4, 40, 80, 120, 255);
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, 4, 4, format, sRgb, isNormal);
        Assert.NotNull(image);
        Assert.NotNull(dds);

        var path = Path.Combine(Path.GetTempPath(), "bc-dds-" + Guid.NewGuid() + ".dds");
        try
        {
            Assert.True(BcDds.Write(path, image!.Value, dds!));
            Assert.True(BcDds.TryRead(path, out var read));
            Assert.Equal(format, read.Format);
            Assert.Equal(sRgb, read.SRgb);
            Assert.Equal(isNormal, read.IsNormal);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BcDds_RoundTrip_PreservesSourceDimensionsWhenPadded()
    {
        var rgba = SolidRgba(6, 6, 255, 0, 0, 255);
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, 6, 6, BcGpuFormat.Dxt5Srgb, sRgb: true, isNormal: false);
        Assert.NotNull(image);
        Assert.NotNull(dds);

        var path = Path.Combine(Path.GetTempPath(), "bc-dds-" + Guid.NewGuid() + ".dds");
        try
        {
            Assert.True(BcDds.Write(path, image!.Value, dds!));
            Assert.True(BcDds.TryRead(path, out var read));
            Assert.Equal(6, read.SourceWidth);
            Assert.Equal(6, read.SourceHeight);
            Assert.Equal(8, read.GpuWidth);
            Assert.Equal(8, read.GpuHeight);
            Assert.True(read.Mips.Length > 1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TextureBcSidecar_RejectsMismatchedImportFlags()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bc-sidecar-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "ao.png");
        var rgba = SolidRgba(4, 4, 128, 128, 128, 255);
        File.WriteAllBytes(src, rgba);

        var sidecar = BcDdsSidecarPath.ForSource(src, sRgb: false);
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, 4, 4, BcGpuFormat.Bc4, sRgb: false, isNormal: false);
        Assert.NotNull(image);
        Assert.NotNull(dds);
        Assert.True(BcDds.Write(sidecar, image!.Value, dds!));

        try
        {
            Assert.False(TextureBcSidecar.SidecarUsable(src, sidecar, sRgb: true, isNormal: false));
            Assert.True(TextureBcSidecar.SidecarUsable(src, sidecar, sRgb: false, isNormal: false));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static byte[] SolidRgba(int width, int height, byte r, byte g, byte b, byte a)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            rgba[i * 4] = r;
            rgba[i * 4 + 1] = g;
            rgba[i * 4 + 2] = b;
            rgba[i * 4 + 3] = a;
        }

        return rgba;
    }
}
