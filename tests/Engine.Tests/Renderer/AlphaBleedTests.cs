using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class AlphaBleedTests
{
    [Fact]
    public void BleedTransparentRgb_CopiesLeafColor_KeepsAlpha()
    {
        const int width = 4;
        const int height = 4;
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            rgba[i * 4] = 255;
            rgba[i * 4 + 1] = 255;
            rgba[i * 4 + 2] = 255;
        }

        var leaf = (1 * width + 1) * 4;
        rgba[leaf] = 0;
        rgba[leaf + 1] = 255;
        rgba[leaf + 2] = 0;
        rgba[leaf + 3] = 255;

        TextureFileDecoder.BleedTransparentRgb(rgba, width, height);

        ((int)rgba[0]).ShouldBe(0);
        ((int)rgba[1]).ShouldBe(255);
        ((int)rgba[3]).ShouldBe(0);
        ((int)rgba[leaf]).ShouldBe(0);
        ((int)rgba[leaf + 1]).ShouldBe(255);
        ((int)rgba[leaf + 3]).ShouldBe(255);
    }
}
