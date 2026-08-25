using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Components;

public class SkyboxComponentTests
{
    [Fact]
    public void Clone_CopiesHdrPathAndIntensity()
    {
        var original = new SkyboxComponent
        {
            HdrPath = "assets/models/sky.hdr",
            Intensity = 2.5f
        };

        var clone = (SkyboxComponent)original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.HdrPath.ShouldBe("assets/models/sky.hdr");
        clone.Intensity.ShouldBe(2.5f);
    }
}
