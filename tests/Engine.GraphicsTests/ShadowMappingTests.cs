using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class ShadowMappingTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void DepthOnlyShadowFramebuffer_IsComplete()
    {
        using var shadowMap = fixture.FrameBufferFactory.Create(new FrameBufferSpecification(1024, 1024)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToBorder
                }
            ])
        });

        shadowMap.GetDepthAttachmentRendererId().ShouldBeGreaterThan(0u);
        shadowMap.GetColorAttachmentRendererId().ShouldBe(0u);
    }

    [GraphicsFact]
    public void Occluder_DarkensFloor_ComparedToUnshadowed()
    {
        var lit = MeanLuminance(withOccluder: false);
        var shadowed = MeanLuminance(withOccluder: true);
        shadowed.ShouldBeLessThan(lit - 0.02f);
    }

    [GraphicsFact]
    public void BeginShadowPass_ReturnsFalse_WhenDirectionalLightIsOff()
    {
        fixture.Graphics3D.SetDirectionalLight(new Vector3(0, -1, 0), Vector3.Zero, 0f);
        fixture.Graphics3D.BeginShadowPass().ShouldBeFalse();
    }

    private float MeanLuminance(bool withOccluder)
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        var camera = new EditorCamera(45f, 1f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);
        camera.SetFocalPoint(Vector3.Zero);
        camera.SetDistance(8f);
        camera.SetPitch(-0.65f);
        camera.SetYaw(0.5f);

        var lightDir = Vector3.Normalize(new Vector3(-0.55f, -1f, -0.15f));
        var floor = Matrix4x4.CreateScale(12f, 0.2f, 12f) * Matrix4x4.CreateTranslation(0f, -1f, 0f);
        var occluder = Matrix4x4.CreateTranslation(0f, 0.25f, 0f);

        framebuffer.Bind();
        fixture.Graphics3D.SetClearColor(new Vector4(0.05f, 0.05f, 0.07f, 1f));
        fixture.Graphics3D.Clear();
        fixture.Graphics3D.SetAmbientLight(Vector3.One, 0.12f);
        fixture.Graphics3D.SetDirectionalLight(lightDir, Vector3.One, 1f);
        fixture.Graphics3D.BeginScene(camera);
        if (fixture.Graphics3D.BeginShadowPass())
        {
            if (withOccluder)
                fixture.Graphics3D.DrawCube(occluder, Vector4.One);
            fixture.Graphics3D.DrawCube(floor, Vector4.One);
            fixture.Graphics3D.EndShadowPass();
        }

        if (withOccluder)
            fixture.Graphics3D.DrawCube(occluder, Vector4.One);
        fixture.Graphics3D.DrawCube(floor, Vector4.One);
        fixture.Graphics3D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        var sum = 0f;
        for (var i = 0; i < pixels.Length; i += 4)
            sum += (pixels[i] + pixels[i + 1] + pixels[i + 2]) / (3f * 255f);
        return sum / (pixels.Length / 4f);
    }
}
