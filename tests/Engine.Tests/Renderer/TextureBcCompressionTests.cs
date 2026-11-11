using Engine.Renderer.Textures.Compression;

namespace Engine.Tests.Renderer;

public class TextureBcCompressionTests
{
    [Fact]
    public void BcFormatPick_OpaqueSrgb_UsesDxt1()
    {
        var rgba = SolidRgba(4, 4, 200, 100, 50, 255);
        Assert.Equal(BcGpuFormat.Dxt1Srgb, BcFormatPick.Pick(sRgb: true, isNormalMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_Alpha_UsesDxt5Srgb()
    {
        var rgba = SolidRgba(4, 4, 200, 100, 50, 128);
        Assert.Equal(BcGpuFormat.Dxt5Srgb, BcFormatPick.Pick(sRgb: true, isNormalMap: false, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_Normal_UsesBc5()
    {
        var rgba = SolidRgba(4, 4, 128, 128, 255, 255);
        Assert.Equal(BcGpuFormat.Bc5, BcFormatPick.Pick(sRgb: false, isNormalMap: true, rgba, 4, 4));
    }

    [Fact]
    public void BcFormatPick_GrayscaleLinear_UsesBc4()
    {
        var rgba = SolidRgba(4, 4, 90, 90, 90, 255);
        Assert.Equal(BcGpuFormat.Bc4, BcFormatPick.Pick(sRgb: false, isNormalMap: false, rgba, 4, 4));
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

        Assert.Equal(BcGpuFormat.Dxt5Linear, BcFormatPick.Pick(sRgb: false, isNormalMap: false, rgba, 4, 4));
    }

    [Fact]
    public void TextureSidecar_IsFresh_WhenSidecarNewer()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bc-test-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "a.png");
        var side = Path.Combine(dir, "a.dds");
        File.WriteAllText(src, "src");
        File.WriteAllText(side, "dds");
        File.SetLastWriteTimeUtc(side, DateTime.UtcNow.AddMinutes(1));
        try
        {
            Assert.True(TextureSidecar.IsFresh(src, side));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void BcDds_RoundTrip_PreservesSourceDimensionsWhenPadded()
    {
        var rgba = SolidRgba(6, 6, 255, 0, 0, 255);
        var format = BcGpuFormat.Dxt5Srgb;
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, 6, 6, format);
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
