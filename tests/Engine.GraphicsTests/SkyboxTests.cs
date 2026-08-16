using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class SkyboxTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void DrawSkybox_WithRedEnvironment_FillsBackground()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sky-{Guid.NewGuid():N}.hdr");
        try
        {
            HdrTestImages.WriteSolidHdr(path, 4, 2, new Vector3(1f, 0f, 0f));
            using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

            var camera = new EditorCamera(45f, 1f, 0.1f, 100f);
            camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

            framebuffer.Bind();
            fixture.Graphics3D.SetClearColor(new Vector4(0f, 0f, 1f, 1f));
            fixture.Graphics3D.Clear();
            fixture.Graphics3D.SetEnvironment(path, 1f);
            fixture.Graphics3D.BeginScene(camera);
            fixture.Graphics3D.DrawSkybox();
            fixture.Graphics3D.EndScene();
            framebuffer.Unbind();

            var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
            var center = (FramebufferTestSpecs.Height / 2 * FramebufferTestSpecs.Width
                          + FramebufferTestSpecs.Width / 2) * 4;
            (pixels[center] / 255f).ShouldBeGreaterThan(0.5f);
            (pixels[center + 2] / 255f).ShouldBeLessThan(0.1f);
        }
        finally
        {
            fixture.Graphics3D.SetEnvironment(null, 1f);
            File.Delete(path);
        }
    }

    [GraphicsFact]
    public void DrawSkybox_WithoutEnvironment_IsNoOp()
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        var camera = new EditorCamera(45f, 1f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        framebuffer.Bind();
        fixture.Graphics3D.SetClearColor(new Vector4(0f, 0f, 1f, 1f));
        fixture.Graphics3D.Clear();
        fixture.Graphics3D.SetEnvironment(null, 1f);
        fixture.Graphics3D.BeginScene(camera);
        fixture.Graphics3D.DrawSkybox();
        fixture.Graphics3D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        var center = (FramebufferTestSpecs.Height / 2 * FramebufferTestSpecs.Width
                      + FramebufferTestSpecs.Width / 2) * 4;
        (pixels[center + 2] / 255f).ShouldBeGreaterThan(0.9f);
    }
}
