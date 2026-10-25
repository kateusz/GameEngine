using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.OpenGL;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class SsaoTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void CreaseDarkensIndirectLight_AndSunlitFaceStays()
    {
        var textures = new TextureFactory();
        var meshes = new MeshFactory(
            textures,
            fixture.VertexArrayFactory,
            fixture.VertexBufferFactory,
            fixture.IndexBufferFactory);
        var graphics = new Graphics3D(
            fixture.RendererApi,
            fixture.ShaderFactory,
            meshes,
            textures,
            fixture.FrameBufferFactory);
        var ssao = new SsaoPass(
            fixture.RendererApi,
            fixture.ShaderFactory,
            fixture.VertexArrayFactory,
            fixture.FrameBufferFactory,
            textures);
        try
        {
            graphics.Init();
            ssao.Available.ShouldBeTrue();

            var off = Draw(graphics, ssao, textures, ambient: 1f, sun: Vector3.Zero, enabled: false);
            var on = Draw(graphics, ssao, textures, ambient: 1f, sun: Vector3.Zero, enabled: true);
            Luminance(on.Crease).ShouldBeLessThanOrEqualTo(Luminance(off.Crease) * 0.95f);

            var sunOff = Draw(graphics, ssao, textures, ambient: 0f, sun: Vector3.One, enabled: false);
            var sunOn = Draw(graphics, ssao, textures, ambient: 0f, sun: Vector3.One, enabled: true);
            var ratio = Luminance(sunOn.Outer) / MathF.Max(1f, Luminance(sunOff.Outer));
            ratio.ShouldBeInRange(0.98f, 1.02f);
        }
        finally
        {
            ssao.Dispose();
            graphics.Dispose();
            meshes.Dispose();
            textures.Dispose();
        }
    }

    private (byte[] Crease, byte[] Outer) Draw(
        Graphics3D graphics, SsaoPass ssao, TextureFactory textures,
        float ambient, Vector3 sun, bool enabled)
    {
        const int width = FramebufferTestSpecs.Width;
        const int height = FramebufferTestSpecs.Height;
        var eye = new Vector3(0.5f, 0.6f, 2.4f);
        var view = Matrix4x4.CreateLookAt(eye, new Vector3(0.5f, 0f, 0.5f), Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 1f, 0.1f, 20f);
        var scene = new SceneView(view * projection, eye, View: view, Projection: projection);

        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        framebuffer.Bind();
        graphics.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        graphics.Clear();
        graphics.SetSkybox(null);
        graphics.SetAmbientLight(Vector3.One, ambient);
        graphics.SetDirectionalLight(new Vector3(0f, 0f, -1f), sun);
        graphics.SetPointLights([]);

        if (enabled)
        {
            ssao.TryOcclude((uint)width, (uint)height, projection, 0.5f, () =>
            {
                graphics.BeginNormalPass(view, scene.ViewProjection);
                try
                {
                    DrawCubes(graphics);
                }
                finally
                {
                    graphics.EndNormalPass();
                }
            }, out var occlusion).ShouldBeTrue();
            graphics.SetSsao(occlusion, 1f);
        }
        else
        {
            graphics.SetSsao(textures.GetWhiteTexture().GetRendererId(), 0f);
        }

        graphics.BeginScene(scene);
        DrawCubes(graphics);
        graphics.EndScene();
        framebuffer.Unbind();

        var pixels = GlFramebufferCapture.ReadColorRgba8(framebuffer);
        Project(scene.ViewProjection, new Vector3(0.5f, 0f, 0.5f), width, height, out var creaseX, out var creaseY)
            .ShouldBeTrue();
        Project(scene.ViewProjection, new Vector3(0f, 0f, 0.5f), width, height, out var outerX, out var outerY)
            .ShouldBeTrue();
        return (Pixel(pixels, width, height, creaseX, creaseY), Pixel(pixels, width, height, outerX, outerY));
    }

    private static void DrawCubes(Graphics3D graphics)
    {
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        graphics.DrawCube(Matrix4x4.CreateTranslation(1f, 0f, 0f), Vector4.One);
    }

    private static bool Project(Matrix4x4 viewProjection, Vector3 world, int width, int height, out int x, out int y)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), viewProjection);
        if (clip.W <= 0f)
        {
            x = y = 0;
            return false;
        }

        var ndcX = clip.X / clip.W;
        var ndcY = clip.Y / clip.W;
        x = (int)((ndcX * 0.5f + 0.5f) * width);
        var glY = (int)((ndcY * 0.5f + 0.5f) * height);
        y = height - 1 - glY;
        return x is >= 0 && y is >= 0 && x < width && y < height;
    }

    private static byte[] Pixel(byte[] pixels, int width, int height, int x, int y)
    {
        var i = (y * width + x) * 4;
        return [pixels[i], pixels[i + 1], pixels[i + 2]];
    }

    private static float Luminance(byte[] rgb) => (rgb[0] + rgb[1] + rgb[2]) / 3f;
}
