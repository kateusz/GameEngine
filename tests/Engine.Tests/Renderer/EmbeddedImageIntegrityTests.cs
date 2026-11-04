using Engine.Renderer.Models.RuntimeMesh;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class EmbeddedImageIntegrityTests
{
    // 1x1 RGB PNG. A one-bit flip in an extracted copy is what stb reports as "invalid filter".
    private static readonly byte[] OnePixelPng =
    [
        137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 2, 0, 0, 0,
        144, 119, 83, 222, 0, 0, 0, 12, 73, 68, 65, 84, 120, 156, 99, 248, 207, 192, 0, 0, 3, 1, 1, 0, 201,
        254, 146, 239, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
    ];

    [Fact]
    public void IsIntactImage_AcceptsPngAndRejectsAFlippedByte()
    {
        ModelSourceTextureSidecar.IsIntactImage(OnePixelPng).ShouldBeTrue();

        var flipped = (byte[])OnePixelPng.Clone();
        flipped[40] ^= 0x40;
        ModelSourceTextureSidecar.IsIntactImage(flipped).ShouldBeFalse();
    }

    [Fact]
    public void IsIntactImage_LeavesNonPngBytesAlone()
    {
        ModelSourceTextureSidecar.IsIntactImage([0xFF, 0xD8, 0xFF, 0xD9]).ShouldBeTrue();
    }
}
