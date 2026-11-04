using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Models;
using Engine.Renderer.Shaders;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Scene;

public class SceneRenderPipelineSsaoTests
{
    [Fact]
    public void RenderScene_SsaoOff_SkipsNormalPass_AndBindsWhiteAtStrengthZero()
    {
        var graphics = new SceneRenderPipelineShadowTests.RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            new Context(), Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(),
            View(ssao: false), Pass());

        graphics.Order.ShouldNotContain("normal");
        graphics.Ssao.ShouldBe((0u, 0f));
        graphics.Order.ShouldContain("begin-scene");
    }

    [Theory]
    [InlineData(0f, 0.5f)]
    [InlineData(1f, 0f)]
    [InlineData(1f, -1f)]
    public void RenderScene_UnusableStrengthOrRadius_SkipsNormalPass(float strength, float radius)
    {
        var graphics = new SceneRenderPipelineShadowTests.RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            new Context(), Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(),
            View(ssao: true, strength, radius), Pass());

        graphics.Order.ShouldNotContain("normal");
        graphics.Ssao.Strength.ShouldBe(0f);
    }

    [Fact]
    public void RenderScene_SsaoOn_RunsNormalPassBeforeColor_AndBindsTheOcclusion()
    {
        var graphics = new SceneRenderPipelineShadowTests.RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            new Context(), Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(),
            View(ssao: true), Pass());

        graphics.Order.IndexOf("normal").ShouldBeLessThan(graphics.Order.IndexOf("begin-scene"));
        graphics.Ssao.ShouldBe((9u, 1f));
    }

    [Fact]
    public void RenderScene_NullPass_BindsStrengthZero()
    {
        var graphics = new SceneRenderPipelineShadowTests.RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            new Context(), Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(),
            View(ssao: true));

        graphics.Order.ShouldNotContain("normal");
        graphics.Ssao.Strength.ShouldBe(0f);
    }

    private static SceneView View(bool ssao, float strength = 1f, float radius = 0.5f) =>
        new(Matrix4x4.Identity, Ssao: ssao, SsaoRadius: radius, SsaoStrength: strength,
            Projection: Matrix4x4.Identity, TargetWidth: 32, TargetHeight: 24);

    private static SsaoPass Pass()
    {
        var renderer = Substitute.For<IRendererAPI>();
        renderer.MaxFragmentTextureImageUnits.Returns(32);
        var shaders = Substitute.For<IShaderFactory>();
        shaders.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        var arrays = Substitute.For<IVertexArrayFactory>();
        arrays.Create().Returns(Substitute.For<IVertexArray>());
        var targets = Substitute.For<IFrameBufferFactory>();
        targets.Create(Arg.Any<FrameBufferSpecification>()).Returns(call =>
        {
            var buffer = Substitute.For<IFrameBuffer>();
            buffer.GetSpecification().Returns(call.Arg<FrameBufferSpecification>());
            buffer.GetColorAttachmentRendererId().Returns(9u);
            return buffer;
        });
        var textures = Substitute.For<ITextureFactory>();
        var noise = Substitute.For<Texture2D>();
        noise.GetRendererId().Returns(3u);
        textures.CreateFromRgba(Arg.Any<byte[]>(), 4, 4).Returns(noise);
        return new SsaoPass(renderer, shaders, arrays, targets, textures);
    }
}
