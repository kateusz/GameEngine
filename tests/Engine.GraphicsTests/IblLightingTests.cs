using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class IblLightingTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    private EditorCamera CreateCamera()
    {
        var camera = new EditorCamera(45f, 1f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);
        camera.SetFocalPoint(Vector3.Zero);
        camera.SetDistance(2.5f);
        return camera;
    }

    private float[] RenderSphereCenterPixel(bool withEnvironment)
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        string? envPath = null;
        if (withEnvironment)
        {
            envPath = Path.Combine(Path.GetTempPath(), $"ibl-{Guid.NewGuid():N}.hdr");
            HdrTestImages.WriteSolidHdr(envPath, 4, 2, new Vector3(1f, 0f, 0f));
        }

        try
        {
            framebuffer.Bind();
            fixture.Graphics3D.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
            fixture.Graphics3D.Clear();
            fixture.Graphics3D.SetAmbientLight(Vector3.One, 0.5f);
            fixture.Graphics3D.SetDirectionalLight(new Vector3(0, -1, 0), Vector3.Zero, 0f);
            fixture.Graphics3D.SetEnvironment(envPath, 1f);
            fixture.Graphics3D.BeginScene(CreateCamera());
            fixture.Graphics3D.DrawBuiltinSphere(Matrix4x4.Identity, Vector4.One, metallic: 1f, roughness: 0.1f);
            fixture.Graphics3D.EndScene();
            framebuffer.Unbind();

            var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
            var center = (FramebufferTestSpecs.Height / 2 * FramebufferTestSpecs.Width
                          + FramebufferTestSpecs.Width / 2) * 4;
            return [pixels[center] / 255f, pixels[center + 1] / 255f, pixels[center + 2] / 255f];
        }
        finally
        {
            fixture.Graphics3D.SetEnvironment(null, 1f);
            if (envPath is not null)
                File.Delete(envPath);
        }
    }

    [GraphicsFact]
    public void MetalSphere_UnderRedSky_ReflectsRed()
    {
        var rgb = RenderSphereCenterPixel(withEnvironment: true);
        rgb[0].ShouldBeGreaterThan(0.3f);
        rgb[1].ShouldBeLessThan(0.08f);
        rgb[2].ShouldBeLessThan(0.08f);
    }

    [GraphicsFact]
    public void MetalSphere_WithoutEnvironment_UsesFlatAmbientFallback()
    {
        var rgb = RenderSphereCenterPixel(withEnvironment: false);
        rgb[0].ShouldBe(rgb[1], 0.02f);
        rgb[1].ShouldBe(rgb[2], 0.02f);
        rgb[0].ShouldBeGreaterThan(0.01f);
        rgb[0].ShouldBeLessThan(0.15f);
    }
}
