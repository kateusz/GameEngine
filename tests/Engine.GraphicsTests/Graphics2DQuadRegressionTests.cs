using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class Graphics2DQuadRegressionTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void SolidQuad_MatchesBaseline()
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        var camera = new SceneCamera();
        camera.SetOrthographic(1f, -1f, 1f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        framebuffer.Bind();
        fixture.Graphics2D.SetClearColor(new Vector4(0.08f, 0.08f, 0.08f, 1f));
        fixture.Graphics2D.Clear();
        fixture.Graphics2D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        fixture.Graphics2D.DrawQuad(Vector3.Zero, new Vector2(1.2f, 0.8f), new Vector4(0.9f, 0.2f, 0.1f, 1f));
        fixture.Graphics2D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        ImageRegressionAssert.MatchesBaseline(
            "quad-solid",
            pixels,
            FramebufferTestSpecs.Width,
            FramebufferTestSpecs.Height);
    }

    [GraphicsFact]
    public void OverlappingQuads_LaterEntityIdWinsPickBuffer()
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        var camera = new SceneCamera();
        camera.SetOrthographic(1f, -1f, 1f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        framebuffer.Bind();
        fixture.Graphics2D.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        fixture.Graphics2D.Clear();
        framebuffer.ClearAttachment(1, -1);
        fixture.Graphics2D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        fixture.Graphics2D.DrawQuad(Matrix4x4.CreateScale(2f), new Vector4(1f, 0f, 0f, 1f), 10);
        fixture.Graphics2D.DrawQuad(Matrix4x4.CreateScale(0.5f), new Vector4(0f, 1f, 0f, 0.5f), 20);
        fixture.Graphics2D.EndScene();

        var entityId = framebuffer.ReadPixel(1, FramebufferTestSpecs.Width / 2, FramebufferTestSpecs.Height / 2);
        framebuffer.Unbind();

        entityId.ShouldBe(20);
    }
}
