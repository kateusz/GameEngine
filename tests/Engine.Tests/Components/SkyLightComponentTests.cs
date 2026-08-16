using SceneComponents.Lighting;
using Shouldly;

namespace Engine.Tests.Components;

public class SkyLightComponentTests
{
    [Fact]
    public void Clone_CopiesAllFields()
    {
        var component = new SkyLightComponent { HdrPath = "assets/sky.hdr", Intensity = 2.5f };

        var clone = (SkyLightComponent)component.Clone();

        clone.ShouldNotBeSameAs(component);
        clone.HdrPath.ShouldBe("assets/sky.hdr");
        clone.Intensity.ShouldBe(2.5f);
    }
}
