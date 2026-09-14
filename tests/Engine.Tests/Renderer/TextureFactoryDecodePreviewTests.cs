using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class TextureFactoryDecodePreviewTests
{
    [Fact]
    public void DecodePreview_BgrTga_ReturnsTightlyPackedRgba()
    {
        var path = WriteTempTga(width: 1, height: 1, bytesPerPixel: 3, pixels: [10, 20, 30]);
        using var factory = new TextureFactory();
        try
        {
            var (data, width, height) = factory.DecodePreview(path);

            width.ShouldBe(1);
            height.ShouldBe(1);
            data.Length.ShouldBe(4);
            data.ShouldBe(new byte[] { 30, 20, 10, 255 });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DecodePreview_BgraTga_ReturnsTightlyPackedRgba()
    {
        var path = WriteTempTga(width: 1, height: 1, bytesPerPixel: 4, pixels: [10, 20, 30, 40]);
        using var factory = new TextureFactory();
        try
        {
            var (data, width, height) = factory.DecodePreview(path);

            width.ShouldBe(1);
            height.ShouldBe(1);
            data.Length.ShouldBe(4);
            data.ShouldBe(new byte[] { 30, 20, 10, 40 });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DecodePreview_LargeBgrTga_DownscalesPackedRgba()
    {
        var pixels = new byte[128 * 64 * 3];
        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = 10;
            pixels[i + 1] = 20;
            pixels[i + 2] = 30;
        }

        var path = WriteTempTga(width: 128, height: 64, bytesPerPixel: 3, pixels: pixels);
        using var factory = new TextureFactory();
        try
        {
            var (data, width, height) = factory.DecodePreview(path);

            width.ShouldBe(64);
            height.ShouldBe(32);
            data.Length.ShouldBe(64 * 32 * 4);
            data.AsSpan(0, 4).ToArray().ShouldBe(new byte[] { 30, 20, 10, 255 });
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempTga(int width, int height, int bytesPerPixel, byte[] pixels)
    {
        var path = Path.Combine(Path.GetTempPath(), $"preview-{Guid.NewGuid():N}.tga");
        var header = new byte[18];
        header[2] = 2;
        header[12] = (byte)width;
        header[13] = (byte)(width >> 8);
        header[14] = (byte)height;
        header[15] = (byte)(height >> 8);
        header[16] = (byte)(bytesPerPixel * 8);
        header[17] = bytesPerPixel == 4 ? (byte)8 : (byte)0;

        using var stream = File.Create(path);
        stream.Write(header);
        stream.Write(pixels);
        return path;
    }
}
