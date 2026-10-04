using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

public class SsaoKernelTests
{
    [Fact]
    public void CreateSsaoKernel_IsHemisphere()
    {
        var kernel = LightingMath.CreateSsaoKernel();

        kernel.Length.ShouldBe(LightingMath.SsaoKernelSize);
        foreach (var sample in kernel)
            sample.Z.ShouldBeGreaterThanOrEqualTo(0f);
    }
}
