using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.PostProcessing;
using Engine.Renderer.Shaders;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class FxaaPassTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly IRendererAPI _rendererApi;
    private readonly IShader _shader;
    private readonly IVertexArray _vao;
    private readonly IFrameBuffer _output;
    private readonly FxaaPass _pass;

    public FxaaPassTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GameEngine-FxaaPassTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "assets"));

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(Path.Combine(_tempRoot, "assets"));
        PathBuilder.UseProjectContext(context);

        _rendererApi = Substitute.For<IRendererAPI>();
        _shader = Substitute.For<IShader>();
        _vao = Substitute.For<IVertexArray>();
        _output = Substitute.For<IFrameBuffer>();
        _output.GetSpecification().Returns(new FrameBufferSpecification(1280, 720)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([])
        });

        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>()).Returns(_shader);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(_vao);

        var fbFactory = Substitute.For<IFrameBufferFactory>();
        fbFactory.Create(Arg.Any<FrameBufferSpecification>()).Returns(_output);

        _pass = new FxaaPass(_rendererApi, shaderFactory, vaoFactory, fbFactory);
    }

    public void Dispose()
    {
        _pass.Dispose();
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void Apply_BindsSourceAndDrawsFullscreenTriangle()
    {
        var result = _pass.Apply(sdrColorAttachmentId: 42, width: 1280, height: 720);

        result.ShouldBe(_output);
        _shader.Received().SetInt("u_Texture", 0);
        _shader.Received().SetFloat("u_InverseWidth", 1f / 1280f);
        _shader.Received().SetFloat("u_InverseHeight", 1f / 720f);
        _rendererApi.Received().BindTexture2D(42, 0);
        _rendererApi.Received().DrawArrays(_vao, 3);
        _output.Received().Bind();
        _output.Received().Unbind();
    }

    [Fact]
    public void ApplyTo_NullTarget_DoesNotBindFramebuffer()
    {
        _pass.ApplyTo(7, sdrTarget: null, width: 100, height: 50);

        _shader.Received().SetFloat("u_InverseWidth", 0.01f);
        _shader.Received().SetFloat("u_InverseHeight", 0.02f);
        _rendererApi.Received().BindTexture2D(7, 0);
        _rendererApi.Received().DrawArrays(_vao, 3);
        _output.DidNotReceive().Bind();
    }
}
