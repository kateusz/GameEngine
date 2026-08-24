using Engine.Platform.SilkNet;
using Engine.Renderer.Exceptions;
using NSubstitute;
using Shouldly;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class GraphicsContextFailureTests
{
    [Fact]
    public void Constructor_WithNullWindow_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new SilkNetGraphicsContext(null!));
    }

    [Fact]
    public void Create_WhenGLFactoryThrows_WrapsInRendererInitializationException()
    {
        var inner = new InvalidOperationException("no GPU");
        var window = Substitute.For<IWindow>();
        var context = new SilkNetGraphicsContext(window, _ => throw inner);

        var ex = Should.Throw<RendererInitializationException>(context.Create);
        ex.InnerException.ShouldBe(inner);
    }

    [GraphicsFact]
    public void Create_WhenAlreadyCreated_ThrowsInvalidOperationException()
    {
        var window = HeadlessWindow.Create("Engine.GraphicsTests.DoubleCreate", new Vector2D<int>(1, 1));
        var context = new SilkNetGraphicsContext(window);
        context.Create();

        Should.Throw<InvalidOperationException>(context.Create);

        context.Dispose();
        window.Dispose();
    }
}
