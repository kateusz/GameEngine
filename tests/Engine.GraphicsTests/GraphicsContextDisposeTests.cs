using Engine.Platform.SilkNet;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class GraphicsContextDisposeTests
{
    [GraphicsFact]
    public void Dispose_ReleasesContext_AndSubsequentCallsFail()
    {
        var window = HeadlessWindow.Create("Engine.GraphicsTests.Dispose");
        var context = new SilkNetGraphicsContext();
        context.Create(window);

        context.Dispose();
        context.IsCreated.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => context.GetError());

        window.Dispose();
    }
}
