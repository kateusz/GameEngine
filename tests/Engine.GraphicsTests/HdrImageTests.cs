using System.Numerics;
using Engine.Renderer.Textures;
using Shouldly;

namespace Engine.GraphicsTests;

public class HdrImageTests
{
    [Fact]
    public void Load_SolidColorHdr_ReturnsDimensionsAndPixels()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hdrimg-{Guid.NewGuid():N}.hdr");
        try
        {
            HdrTestImages.WriteSolidHdr(path, 4, 2, new Vector3(1f, 0.25f, 0.5f));

            var image = HdrImage.Load(path);

            image.Width.ShouldBe(4);
            image.Height.ShouldBe(2);
            image.Pixels.Length.ShouldBe(4 * 2 * 3);
            image.Pixels[0].ShouldBe(1f, 0.02f);
            image.Pixels[1].ShouldBe(0.25f, 0.02f);
            image.Pixels[2].ShouldBe(0.5f, 0.02f);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
