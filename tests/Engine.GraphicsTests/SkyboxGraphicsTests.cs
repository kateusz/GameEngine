using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.OpenGL;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class SkyboxGraphicsTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void DrawSkybox_FillsFramebufferWithNonBlackPixels()
    {
        var hdrPath = Path.Combine(AppContext.BaseDirectory, "assets", "textures", "sky.hdr");
        File.Exists(hdrPath).ShouldBeTrue($"HDR test asset missing: {hdrPath}");

        var textureFactory = new TextureFactory();
        var hdr = textureFactory.Create(hdrPath, sRgb: false);

        var vertexArrayFactory = new VertexArrayFactory();
        var meshFactory = new MeshFactory(textureFactory, vertexArrayFactory, new VertexBufferFactory(),
            new IndexBufferFactory());
        var graphics3D = new Graphics3D(fixture.RendererApi, fixture.ShaderFactory, meshFactory, textureFactory,
            vertexArrayFactory, fixture.FrameBufferFactory);
        graphics3D.Init();

        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());

        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4f, 0.1f, 100f);
        camera.SetViewportSize(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);

        framebuffer.Bind();
        graphics3D.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        graphics3D.Clear();
        graphics3D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        graphics3D.DrawSkybox(hdr, 1f, 0f);
        graphics3D.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        var meanLuminance = 0f;
        for (var i = 0; i < pixels.Length; i += 4)
            meanLuminance += (pixels[i] + pixels[i + 1] + pixels[i + 2]) / (3f * 255f);
        meanLuminance /= pixels.Length / 4;

        meanLuminance.ShouldBeGreaterThan(0.05f);
    }
}
