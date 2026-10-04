using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class EntityIdAttachmentTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void SolidQuad_WritesEntityIdToPickAttachment()
    {
        const int entityId = 17;
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        var camera = new SceneCamera();
        camera.SetOrthographic(1f, -1f, 1f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        framebuffer.Bind();
        fixture.Graphics2D.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        fixture.Graphics2D.Clear();
        framebuffer.ClearAttachment(1, -1);
        fixture.Graphics2D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        fixture.Graphics2D.DrawQuad(Matrix4x4.Identity, new Vector4(1f, 0f, 0f, 1f), entityId);
        fixture.Graphics2D.EndScene();
        framebuffer.Unbind();

        var x = FramebufferTestSpecs.Width / 2;
        var y = FramebufferTestSpecs.Height / 2;
        framebuffer.ReadPixel(1, x, y).ShouldBe(entityId);
    }
}
