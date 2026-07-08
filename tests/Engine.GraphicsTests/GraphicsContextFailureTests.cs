using Engine.Platform.SilkNet;
using Engine.Renderer.Exceptions;
using NSubstitute;
using Shouldly;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

[Trait("Category", "Unit")]
[Collection("GraphicsIntegration")]
public class GraphicsContextFailureTests
{
    [Fact]
    public void Create_WithNullWindow_ThrowsArgumentNullException()
    {
        var context = new SilkNetGraphicsContext();

        Should.Throw<ArgumentNullException>(() => context.Create(null!));
    }

    [Fact]
    public void Create_WhenGLFactoryThrows_WrapsInRendererInitializationException()
    {
        var inner = new InvalidOperationException("no GPU");
        var context = new SilkNetGraphicsContext(_ => throw inner);
        var window = Substitute.For<IWindow>();

        var ex = Should.Throw<RendererInitializationException>(() => context.Create(window));
        ex.InnerException.ShouldBe(inner);
    }

    [GraphicsFact]
    public void Create_WhenAlreadyCreated_ThrowsInvalidOperationException()
    {
        var options = WindowOptions.Default;
        options.IsVisible = false;
        options.Title = "Engine.GraphicsTests.DoubleCreate";
        options.Size = new Vector2D<int>(1, 1);

        var window = Silk.NET.Windowing.Window.Create(options);
        window.Initialize();

        var context = new SilkNetGraphicsContext();
        context.Create(window);

        Should.Throw<InvalidOperationException>(() => context.Create(window));

        context.Dispose();
        window.Dispose();
    }
}
