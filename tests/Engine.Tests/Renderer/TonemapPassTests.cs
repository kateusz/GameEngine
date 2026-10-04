using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class TonemapPassTests
{
    [Fact]
    public void Resolve_ZeroSize_DoesNotDraw_AndIsNullBeforeAnyDraw()
    {
        var pass = NewPass(out var renderer, out _, out _);
        var scene = Scene(0, 0);

        pass.Resolve(scene).ShouldBeNull();
        renderer.DidNotReceive().DrawArrays(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    [Fact]
    public void Resolve_SecondCall_DoesNotCreateTheShaderAgain()
    {
        var pass = NewPass(out _, out var shaders, out var targets);
        var scene = Scene(32, 24);
        var output = Substitute.For<IFrameBuffer>();
        output.GetSpecification().Returns(new FrameBufferSpecification(32, 24));
        targets.Create(Arg.Any<FrameBufferSpecification>()).Returns(output);

        pass.Resolve(scene);
        pass.Resolve(scene);

        shaders.Received(1).Create(ShaderId.Tonemap);
    }

    [Fact]
    public void Resolve_ShaderCreateThrows_StaysUnavailable_AndDoesNotDraw()
    {
        var shaders = Substitute.For<IShaderFactory>();
        shaders.Create(ShaderId.Tonemap).Returns(_ => throw new InvalidOperationException("no shader"));
        var renderer = Substitute.For<IRendererAPI>();
        var pass = new TonemapPass(
            renderer,
            shaders,
            Substitute.For<IVertexArrayFactory>(),
            Substitute.For<IFrameBufferFactory>());

        pass.Available.ShouldBeFalse();
        pass.Resolve(Scene(8, 8)).ShouldBeNull();
        renderer.DidNotReceive().DrawArrays(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    private static TonemapPass NewPass(
        out IRendererAPI renderer, out IShaderFactory shaders, out IFrameBufferFactory targets)
    {
        renderer = Substitute.For<IRendererAPI>();
        shaders = Substitute.For<IShaderFactory>();
        shaders.Create(ShaderId.Tonemap).Returns(Substitute.For<IShader>());
        var arrays = Substitute.For<IVertexArrayFactory>();
        arrays.Create().Returns(Substitute.For<IVertexArray>());
        targets = Substitute.For<IFrameBufferFactory>();
        return new TonemapPass(renderer, shaders, arrays, targets);
    }

    private static IFrameBuffer Scene(uint width, uint height)
    {
        var scene = Substitute.For<IFrameBuffer>();
        scene.GetSpecification().Returns(new FrameBufferSpecification(width, height));
        scene.GetColorAttachmentRendererId().Returns(4u);
        return scene;
    }
}
