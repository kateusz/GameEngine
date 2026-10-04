using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

public class SsaoPassTests
{
    [Fact]
    public void Available_ShaderCreateThrows_StaysUnavailable_AndDoesNotDraw()
    {
        var renderer = Renderer(32);
        var shaders = Substitute.For<IShaderFactory>();
        shaders.Create(Arg.Any<ShaderId>()).Returns(_ => throw new InvalidOperationException("no shader"));
        var pass = new SsaoPass(renderer, shaders, Arrays(), Targets(), Textures());

        pass.Available.ShouldBeFalse();
        pass.TryOcclude(8, 8, Matrix4x4.Identity, 0.5f, () => { }, out _).ShouldBeFalse();
        renderer.DidNotReceive().DrawArrays(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    [Fact]
    public void Available_FewerThan16Units_StaysUnavailable_AndDoesNotDraw()
    {
        var renderer = Renderer(15);
        var pass = new SsaoPass(renderer, Shaders(), Arrays(), Targets(), Textures());

        pass.Available.ShouldBeFalse();
        pass.TryOcclude(8, 8, Matrix4x4.Identity, 0.5f, () => { }, out _).ShouldBeFalse();
        renderer.DidNotReceive().DrawArrays(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    [Fact]
    public void TryOcclude_ZeroSize_DoesNotDraw()
    {
        var renderer = Renderer(32);
        var pass = new SsaoPass(renderer, Shaders(), Arrays(), Targets(), Textures());

        pass.TryOcclude(0, 24, Matrix4x4.Identity, 0.5f, () => { }, out _).ShouldBeFalse();
        renderer.DidNotReceive().DrawArrays(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    [Fact]
    public void TryOcclude_FullSize_DrawsTwice_AndCreatesShadersOnce()
    {
        var renderer = Renderer(32);
        var shaders = Shaders();
        var targets = Targets();
        var pass = new SsaoPass(renderer, shaders, Arrays(), targets, Textures());

        pass.TryOcclude(32, 24, Matrix4x4.Identity, 0.5f, () => { }, out var occlusion).ShouldBeTrue();
        pass.TryOcclude(32, 24, Matrix4x4.Identity, 0.5f, () => { }, out _).ShouldBeTrue();

        occlusion.ShouldBe(9u);
        shaders.Received(1).Create(ShaderId.Ssao);
        shaders.Received(1).Create(ShaderId.SsaoBlur);
        renderer.Received(4).DrawArrays(Arg.Any<IVertexArray>(), 3);
        targets.Received().Create(Arg.Is<FrameBufferSpecification>(spec =>
            spec.Width == 32 && spec.Height == 24 &&
            spec.AttachmentsSpec.Attachments.Any(a => a.TextureFormat == FrameBufferTextureFormat.RGBA16F) &&
            spec.AttachmentsSpec.Attachments.Any(a => a.TextureFormat == FrameBufferTextureFormat.Depth)));
        targets.Received(2).Create(Arg.Is<FrameBufferSpecification>(spec =>
            spec.Width == 32 && spec.Height == 24 &&
            spec.AttachmentsSpec.Attachments.Count == 1 &&
            spec.AttachmentsSpec.Attachments[0].TextureFormat == FrameBufferTextureFormat.RGBA8));
    }

    [Fact]
    public void TryOcclude_ResizeThrows_DoesNotDrawThatFrame()
    {
        var renderer = Renderer(32);
        var targets = Targets(resizeThrows: true);
        var pass = new SsaoPass(renderer, Shaders(), Arrays(), targets, Textures());

        pass.TryOcclude(32, 24, Matrix4x4.Identity, 0.5f, () => { }, out _).ShouldBeTrue();
        pass.TryOcclude(64, 24, Matrix4x4.Identity, 0.5f, () => { }, out var occlusion).ShouldBeFalse();

        occlusion.ShouldBe(0u);
        renderer.Received(2).DrawArrays(Arg.Any<IVertexArray>(), 3);
    }

    private static IRendererAPI Renderer(int units)
    {
        var renderer = Substitute.For<IRendererAPI>();
        renderer.MaxFragmentTextureImageUnits.Returns(units);
        return renderer;
    }

    private static IShaderFactory Shaders()
    {
        var shaders = Substitute.For<IShaderFactory>();
        shaders.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        return shaders;
    }

    private static IVertexArrayFactory Arrays()
    {
        var arrays = Substitute.For<IVertexArrayFactory>();
        arrays.Create().Returns(Substitute.For<IVertexArray>());
        return arrays;
    }

    private static IFrameBufferFactory Targets(bool resizeThrows = false)
    {
        var targets = Substitute.For<IFrameBufferFactory>();
        targets.Create(Arg.Any<FrameBufferSpecification>()).Returns(call =>
        {
            var spec = call.Arg<FrameBufferSpecification>();
            var buffer = Substitute.For<IFrameBuffer>();
            buffer.GetSpecification().Returns(spec);
            buffer.GetColorAttachmentRendererId().Returns(9u);
            if (resizeThrows)
                buffer.When(b => b.Resize(Arg.Any<uint>(), Arg.Any<uint>()))
                    .Do(_ => throw new InvalidOperationException("resize"));
            return buffer;
        });
        return targets;
    }

    private static ITextureFactory Textures()
    {
        var textures = Substitute.For<ITextureFactory>();
        var noise = Substitute.For<Texture2D>();
        noise.GetRendererId().Returns(3u);
        textures.CreateFromRgba(Arg.Any<byte[]>(), 4, 4).Returns(noise);
        return textures;
    }
}
