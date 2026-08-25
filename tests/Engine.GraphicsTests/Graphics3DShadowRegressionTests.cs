using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.OpenGL;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class Graphics3DShadowRegressionTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void DirectionalShadow_DarkensFloorUnderOccluder()
    {
        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        using var textureFactory = new TextureFactory();
        using var meshFactory = new MeshFactory(
            textureFactory,
            fixture.VertexArrayFactory,
            fixture.VertexBufferFactory,
            fixture.IndexBufferFactory);
        using var graphics3D = new Graphics3D(
            fixture.RendererApi,
            fixture.ShaderFactory,
            meshFactory,
            textureFactory,
            fixture.VertexArrayFactory,
            fixture.FrameBufferFactory);
        graphics3D.Init();

        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        var lightSpace = LightSpaceMatrix.Create(new Vector3(0, -1, 0), Vector3.Zero, 20f);
        var floor = Matrix4x4.CreateScale(10f, 1f, 10f) * Matrix4x4.CreateTranslation(0f, -0.5f, 0f);
        var cube = Matrix4x4.CreateTranslation(0f, 2f, 0f);

        framebuffer.Bind();
        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1f));
        graphics3D.Clear();

        graphics3D.BeginShadowPass(lightSpace).ShouldBeTrue();
        graphics3D.DrawCube(floor, Vector4.One);
        graphics3D.DrawCube(cube, Vector4.One);
        graphics3D.EndShadowPass();
        graphics3D.SetDirectionalLight(new Vector3(0, -1, 0), Vector3.One, lightSpace);

        graphics3D.BeginScene(CameraViews.From(camera, Matrix4x4.CreateTranslation(0f, 4f, 12f)));
        graphics3D.SetAmbientLight(new Vector3(0.2f, 0.2f, 0.2f), 0.1f);
        graphics3D.DrawCube(floor, new Vector4(0.8f, 0.8f, 0.8f, 1f));
        graphics3D.DrawCube(cube, new Vector4(0.9f, 0.9f, 0.9f, 1f));
        graphics3D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        var shadowLuma = SampleLuma(pixels, FramebufferTestSpecs.Width / 2, FramebufferTestSpecs.Height * 3 / 4);
        var litLuma = SampleLuma(pixels, FramebufferTestSpecs.Width / 2, FramebufferTestSpecs.Height / 4);
        shadowLuma.ShouldBeLessThan(litLuma * 0.85f);
    }

    private static float SampleLuma(byte[] pixels, int x, int y)
    {
        var i = (y * FramebufferTestSpecs.Width + x) * 4;
        return 0.2126f * pixels[i] + 0.7152f * pixels[i + 1] + 0.0722f * pixels[i + 2];
    }
}
