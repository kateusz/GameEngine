using Engine.Platform.SilkNet;
using Silk.NET.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class GraphicsContextInitTests(HeadlessGraphicsContextFixture fixture) : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Create_OnHeadlessBackend_SetsIsCreatedTrue()
    {
        fixture.GraphicsContext.IsCreated.ShouldBeTrue();
    }

    [GraphicsFact]
    public void Create_PostInit_IsCleanAndEntryPointsLoad()
    {
        var gl = SilkNetContext.GL;
        gl.ShouldNotBeNull();

        gl.GetStringS(GLEnum.Vendor).ShouldNotBeNull();
        gl.GetError().ShouldBe(GLEnum.NoError);

        gl.GetStringS(GLEnum.Renderer).ShouldNotBeNull();
        gl.GetStringS(GLEnum.Version).ShouldNotBeNullOrWhiteSpace();
        gl.GetError().ShouldBe(GLEnum.NoError);

        fixture.RendererApi.SetViewport(0, 0, 1, 1);
        fixture.RendererApi.GetError().ShouldBe(0);
        gl.ClearColor(0, 0, 0, 1);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        gl.GetError().ShouldBe(GLEnum.NoError);
    }

    [GraphicsFact]
    public void Dispose_ReleasesContext()
    {
        var window = HeadlessWindow.Create("Engine.GraphicsTests.Dispose");
        var context = new SilkNetGraphicsContext(window);
        context.Create();

        context.Dispose();
        context.IsCreated.ShouldBeFalse();

        window.Dispose();
    }
}
