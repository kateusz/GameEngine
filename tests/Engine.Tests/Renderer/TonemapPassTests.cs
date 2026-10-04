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

    [Fact]
    public void Resolve_FullSize_BlursTenTimesThenTonemaps_AndCreatesBloomShadersOnce()
    {
        var blur = Substitute.For<IShader>();
        var pass = NewPass(out var renderer, out var shaders, out var targets, blur);
        var scene = Scene(32, 24);

        pass.Resolve(scene);
        pass.Resolve(scene);

        shaders.Received(1).Create(ShaderId.BloomExtract);
        shaders.Received(1).Create(ShaderId.BloomBlur);
        renderer.Received(24).DrawArrays(Arg.Any<IVertexArray>(), 3);
        blur.Received().SetInt("u_Horizontal", 1);
        blur.Received().SetInt("u_Horizontal", 0);
        targets.Received().Create(Arg.Is<FrameBufferSpecification>(spec => spec.Width == 32 && spec.Height == 24));
    }

    private static TonemapPass NewPass(
        out IRendererAPI renderer,
        out IShaderFactory shaders,
        out IFrameBufferFactory targets,
        IShader? blur = null)
    {
        renderer = Substitute.For<IRendererAPI>();
        shaders = Substitute.For<IShaderFactory>();
        shaders.Create(ShaderId.Tonemap).Returns(Substitute.For<IShader>());
        shaders.Create(ShaderId.BloomExtract).Returns(Substitute.For<IShader>());
        shaders.Create(ShaderId.BloomBlur).Returns(blur ?? Substitute.For<IShader>());
        var arrays = Substitute.For<IVertexArrayFactory>();
        arrays.Create().Returns(Substitute.For<IVertexArray>());
        targets = Substitute.For<IFrameBufferFactory>();
        targets.Create(Arg.Any<FrameBufferSpecification>()).Returns(call =>
        {
            var spec = call.Arg<FrameBufferSpecification>();
            var buffer = Substitute.For<IFrameBuffer>();
            buffer.GetSpecification().Returns(spec);
            buffer.GetColorAttachmentRendererId().Returns(1u);
            return buffer;
        });
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
