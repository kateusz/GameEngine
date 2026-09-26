using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class Bc3EncoderTests
{
    [Fact]
    public void Encode_BlockCount_IsOneBytePerPixel()
    {
        var rgba = new byte[4 * 4 * 4];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = 255;
            rgba[i + 3] = 255;
        }

        var blocks = Bc3Encoder.Encode(rgba, 4, 4);

        blocks.Length.ShouldBe(16);
        blocks[0].ShouldBe((byte)255);
    }
}
