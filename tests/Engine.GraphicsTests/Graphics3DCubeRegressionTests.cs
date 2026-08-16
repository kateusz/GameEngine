using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Scene.Cameras;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class Graphics3DCubeRegressionTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void LitCube_MatchesBaseline()
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        var camera = new EditorCamera(45f, 1f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);
        camera.SetFocalPoint(Vector3.Zero);
        camera.SetDistance(4.5f);
        camera.SetPitch(-0.35f);
        camera.SetYaw(0.75f);

        framebuffer.Bind();
        fixture.Graphics3D.SetClearColor(new Vector4(0.05f, 0.05f, 0.07f, 1f));
        fixture.Graphics3D.Clear();
        fixture.Graphics3D.SetAmbientLight(new Vector3(1f, 1f, 1f), 0.25f);
        fixture.Graphics3D.SetDirectionalLight(Vector3.Normalize(new Vector3(-0.4f, -0.8f, -0.3f)), new Vector3(1f, 0.95f, 0.9f), 1f);
        fixture.Graphics3D.BeginScene(camera);
        fixture.Graphics3D.DrawCube(Matrix4x4.CreateRotationY(0.6f) * Matrix4x4.CreateRotationX(0.25f), new Vector4(0.2f, 0.55f, 0.95f, 1f));
        fixture.Graphics3D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        ImageRegressionAssert.MatchesBaseline(
            "cube-lit",
            pixels,
            FramebufferTestSpecs.Width,
            FramebufferTestSpecs.Height);
    }
}
