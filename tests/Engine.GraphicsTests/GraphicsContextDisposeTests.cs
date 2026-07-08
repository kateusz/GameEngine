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
        var options = WindowOptions.Default;
        options.IsVisible = false;
        options.Title = "Engine.GraphicsTests.Dispose";
        options.Size = new Vector2D<int>(64, 64);

        var window = Window.Create(options);
        window.Initialize();

        var context = new SilkNetGraphicsContext();
        context.Create(window);
        var gl = SilkNetContext.GL;

        context.Dispose();
        context.IsCreated.ShouldBeFalse();

        var failed = false;
        try
        {
            gl.GetError();
        }
        catch
        {
            failed = true;
        }

        if (!failed)
        {
            try
            {
                context.GetError();
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }
        }

        failed.ShouldBeTrue();

        window.Dispose();
    }
}
