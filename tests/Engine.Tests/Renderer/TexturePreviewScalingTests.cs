using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class TexturePreviewScalingTests
{
    [Fact]
    public void DownscaleRgba_4x2_ToMaxEdge2_Produces2x1WithNearestSample()
    {
        var src = new byte[]
        {
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255,
            0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255
        };

        var (data, width, height) = TexturePreviewScaling.DownscaleRgba(src, 4, 2, maxEdge: 2);

        width.ShouldBe(2);
        height.ShouldBe(1);
        data.ShouldBe(new byte[] { 255, 0, 0, 255, 255, 0, 0, 255 });
    }

    [Fact]
    public void DownscaleRgba_WhenAlreadySmall_ReturnsSourceUnchanged()
    {
        var src = new byte[] { 10, 20, 30, 40 };

        var (data, width, height) = TexturePreviewScaling.DownscaleRgba(src, 1, 1, maxEdge: 64);

        width.ShouldBe(1);
        height.ShouldBe(1);
        ReferenceEquals(data, src).ShouldBeTrue();
    }
}
