using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Shouldly;
using Engine.Platform.OpenGL;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class IblMetalTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void MetalCube_InShadow_IsBrighterThanFlatAmbient()
    {
        var hdr = Path.Combine(Path.GetTempPath(), $"ibl-{Guid.NewGuid():N}.hdr");
        WritePlusZHdr(hdr);
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
        try
        {
            graphics.Init();
            graphics.SetSkybox(hdr);
            var withSky = DrawMetal(graphics);
            graphics.SetSkybox(null);
            var flat = DrawMetal(graphics);
            MaxChannel(withSky).ShouldBeGreaterThan(MaxChannel(flat) + 19);
        }
        finally
        {
            graphics.Dispose();
            meshes.Dispose();
            textures.Dispose();
            File.Delete(hdr);
        }
    }

    private byte[] DrawMetal(Graphics3D graphics)
    {
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 3f, 0.1f, 50f);
        camera.SetViewportSize((uint)FramebufferTestSpecs.Width, (uint)FramebufferTestSpecs.Height);
        var view = CameraViews.From(camera, Matrix4x4.CreateTranslation(0f, 0f, 5f));

        using var framebuffer = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        framebuffer.Bind();
        graphics.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        graphics.Clear();
        graphics.SetDirectionalLight(new Vector3(0f, -1f, 0f), Vector3.Zero);
        graphics.BeginScene(view);
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One, metallic: 1f, roughness: 0f, ao: 1f);
        framebuffer.Unbind();
        return GlFramebufferCapture.ReadColorRgba8(framebuffer);
    }

    private static int MaxChannel(byte[] pixels)
    {
        var x = FramebufferTestSpecs.Width / 2;
        var y = FramebufferTestSpecs.Height / 2;
        var i = (y * FramebufferTestSpecs.Width + x) * 4;
        return System.Math.Max(pixels[i], System.Math.Max(pixels[i + 1], pixels[i + 2]));
    }

    private static void WritePlusZHdr(string path)
    {
        const int width = 64;
        const int height = 32;
        var rgb = new float[width * height * 3];
        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;
            var fileY = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;
                var lat = (v - 0.5f) / 0.3183f;
                var phi = (u - 0.5f) / 0.1591f;
                var cosLat = MathF.Cos(lat);
                var dir = new Vector3(MathF.Cos(phi) * cosLat, MathF.Sin(lat), MathF.Sin(phi) * cosLat);
                var plusZ = dir.Z > 0f
                    && MathF.Abs(dir.Z) >= MathF.Abs(dir.X)
                    && MathF.Abs(dir.Z) >= MathF.Abs(dir.Y);
                var value = plusZ ? 4f : 0f;
                var i = (fileY * width + x) * 3;
                rgb[i] = value;
                rgb[i + 1] = value;
                rgb[i + 2] = value;
            }
        }

        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);
        writer.NewLine = "\n";
        writer.WriteLine("#?RADIANCE");
        writer.WriteLine("FORMAT=32-bit_rle_rgbe");
        writer.WriteLine("");
        writer.WriteLine($"-Y {height} +X {width}");
        writer.Flush();
        for (var i = 0; i < rgb.Length; i += 3)
            stream.Write(EncodeRgbe(rgb[i], rgb[i + 1], rgb[i + 2]));
    }

    private static byte[] EncodeRgbe(float r, float g, float b)
    {
        var max = MathF.Max(r, MathF.Max(g, b));
        if (max < 1e-32f)
            return [0, 0, 0, 0];

        var exponent = (int)MathF.Floor(MathF.Log2(max)) + 1;
        var factor = MathF.Pow(2f, 8 - exponent);
        return
        [
            (byte)(r * factor),
            (byte)(g * factor),
            (byte)(b * factor),
            (byte)(exponent + 128)
        ];
    }
}
