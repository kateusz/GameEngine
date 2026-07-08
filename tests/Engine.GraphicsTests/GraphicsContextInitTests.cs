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
    public void Create_OnHeadlessBackend_LogsGLVersionString()
    {
        var version = fixture.GraphicsContext.GetVersionString();
        version.ShouldNotBeNull();

        var gl = SilkNetContext.GL;
        gl.ShouldNotBeNull();
        _ = gl.GetStringS(GLEnum.Vendor);
        _ = gl.GetStringS(GLEnum.Renderer);
        _ = gl.GetStringS(GLEnum.Version);
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);
    }

    [GraphicsFact]
    public void Create_OnHeadlessBackend_LeavesNoGLError()
    {
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);
    }

    [GraphicsFact]
    public void Create_ThenCallGLEntryPoints_LoadsFunctionPointers()
    {
        var gl = SilkNetContext.GL;

        gl.GetStringS(GLEnum.Vendor).ShouldNotBeNull();
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);

        gl.GetStringS(GLEnum.Renderer).ShouldNotBeNull();
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);

        gl.Viewport(0, 0, 1, 1);
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);

        gl.ClearColor(0, 0, 0, 1);
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);

        gl.Clear(ClearBufferMask.ColorBufferBit);
        fixture.GraphicsContext.GetError().ShouldBe((int)GLEnum.NoError);
    }
}
