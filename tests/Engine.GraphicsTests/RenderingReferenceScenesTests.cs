using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ECS;
using Engine.Core.DI;
using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;
using NSubstitute;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

/// <summary>Procedural reference scenes, independent of user projects and importer versions.</summary>
[Collection("GraphicsIntegration")]
[Trait("Category", "GraphicsIntegration")]
public class RenderingReferenceScenesTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    private const uint Width = 96, Height = 64;
    private static readonly string[] Scenarios =
        ["hdri", "directional", "point-cutout", "instances", "lod", "merging", "quality-change", "scene-change"];

    [GraphicsFact]
    public void CaptureReferenceScenes_HdrDisplayPickingAndDrawPaths()
    {
        var root = Environment.GetEnvironmentVariable("RENDER_BASELINE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "TestResults", "RenderingReference");
        Directory.CreateDirectory(root);
        var gl = SilkNetContext.GL;
        File.WriteAllText(Path.Combine(root, "run.json"), JsonSerializer.Serialize(new
        {
            CaptureVersion = 2, Width, Height, Camera = new[] { 0f, 1.5f, 6f }, Target = Vector3.Zero,
            VerticalFov = MathF.PI / 3, Near = 0.1f, Far = 50f,
            Clear = new[] { 0.02f, 0.03f, 0.04f, 1f },
            Tonemap = "Reinhard + gamma 1/2.2; bloom on; FXAA off",
            HdrFormat = "RGBA float32 little-endian, bottom row first",
            DisplayFormat = "RGBA8, top row first",
            Vendor = gl.GetStringS(StringName.Vendor), Renderer = gl.GetStringS(StringName.Renderer),
            Version = gl.GetStringS(StringName.Version), Scenarios
        }, new JsonSerializerOptions { WriteIndented = true }));

        foreach (var scenario in Scenarios)
            Capture(root, scenario);
    }

    private void Capture(string root, string scenario)
    {
        using var textures = new TextureFactory(new EngineHostOptions("reference", (int)Width, (int)Height));
        using var meshes = new MeshFactory(textures, fixture.VertexArrayFactory,
            fixture.VertexBufferFactory, fixture.IndexBufferFactory);
        using var graphics = new Graphics3D(fixture.RendererApi, fixture.ShaderFactory, meshes, fixture.FrameBufferFactory);
        using var tonemap = new TonemapPass(fixture.RendererApi, fixture.ShaderFactory,
            fixture.VertexArrayFactory, fixture.FrameBufferFactory);
        using var hdr = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(Width, Height));
        using var mesh = ReferenceMesh(scenario == "lod");
        using var alpha = textures.CreateFromRgba(
            [255,255,255,0, 255,255,255,255, 255,255,255,255, 255,255,255,0], 2, 2);
        if (scenario == "point-cutout")
        {
            mesh.DiffuseTexture = alpha;
            mesh.AlphaCutout = true;
        }
        using var model = new Model("reference", [mesh]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);
        using var pipeline = new SceneRenderPipeline(fixture.Graphics2D, graphics, textures, models,
            scenario == "merging" ? fixture.VertexArrayFactory : null,
            scenario == "merging" ? fixture.VertexBufferFactory : null,
            scenario == "merging" ? fixture.IndexBufferFactory : null);
        var context = MakeScene(scenario, 10);
        // Environment/LUT preparation runs during Init/SetSkybox, before the first frame.
        // Give it the same explicit initial state regardless of test execution order.
        fixture.RendererApi.SetCullFrontFaces(false);
        fixture.RendererApi.SetFaceCulling(true);
        fixture.RendererApi.SetDepthTest(true);
        fixture.RendererApi.SetDepthWrite(true);
        fixture.RendererApi.SetBlend(true);
        fixture.RendererApi.SetClearColor(new(0.02f, 0.03f, 0.04f, 1));
        graphics.Init();
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 3f, 0.1f, 50f);
        camera.SetViewportSize(Width, Height);
        Matrix4x4.Invert(Matrix4x4.CreateLookAt(new(0, 1.5f, 6), Vector3.Zero, Vector3.UnitY), out var cameraWorld);
        var view = CameraViews.From(camera, cameraWorld) with
        {
            ViewportHeight = Height,
            DirectionalShadows = scenario is "directional" or "quality-change",
            PointShadows = scenario == "point-cutout"
        };
        var skyPath = Path.Combine(root, "reference.hdr");
        if (scenario == "hdri")
        {
            WriteConstantHdr(skyPath);
            var decoded = TextureFileDecoder.DecodeHdr(skyPath)!.Value;
            decoded.Rgb[0].ShouldBe(2f);
            graphics.SetSkybox(skyPath);
        }

        try
        {
            for (var frame = 0; frame < 3; frame++)
            {
                if (frame == 2 && scenario == "quality-change")
                    graphics.SetShadowQuality(new ShadowQuality(DirectionalResolution: 2048));
                if (frame == 2 && scenario == "scene-change")
                {
                    context = MakeScene(scenario, 20);
                            }
                graphics.ResetStats();
                fixture.RendererApi.SetBlend(true);
                fixture.RendererApi.SetFaceCulling(true);
                fixture.RendererApi.SetDepthWrite(true);
                hdr.Bind();
                try
                {
                    graphics.SetClearColor(new(0.02f, 0.03f, 0.04f, 1));
                    graphics.Clear();
                    hdr.ClearAttachment(1, -1);
                    pipeline.RenderScene(context, view);
                }
                finally { hdr.Unbind(); }

                var name = $"{scenario}-{frame}";
                WriteHdr(Path.Combine(root, name + ".rgba32f"), hdr);
                var display = tonemap.Resolve(hdr);
                display.ShouldNotBeNull();
                File.WriteAllBytes(Path.Combine(root, name + ".rgba8"), GlFramebufferCapture.ReadColorRgba8(display));
                var ids = ReadIds(hdr);
                File.WriteAllBytes(Path.Combine(root, name + ".ids"), MemoryMarshal.AsBytes(ids.AsSpan()).ToArray());
                File.WriteAllText(Path.Combine(root, name + ".json"), JsonSerializer.Serialize(new
                {
                    Scenario = scenario, Frame = frame, View = view, Quality = graphics.ShadowQuality,
                    Stats = graphics.GetStats(), PickedIds = ids.Distinct().Order().ToArray()
                }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
                if (frame == 1 && scenario == "instances")
                    graphics.GetStats().InstancedDraws.ShouldBeGreaterThan(0u);
                if (frame == 1 && scenario == "merging")
                    graphics.GetStats().MergedGroups.ShouldBeGreaterThan(0);
                if (scenario == "lod")
                    MeshLod.Select(mesh, Matrix4x4.Identity, view.ViewPosition, view, 1).ShouldBe(1);
                // Stage 1 acceptance: actual scene geometry must reach the HDR ID attachment,
                // including cold shadow maps, quality changes and resource/scene retirement.
                var expectedId = scenario == "scene-change" && frame == 2 ? 20 : 10;
                ids.Contains(expectedId).ShouldBeTrue($"{name}: missing entity {expectedId} in HDR target");
                if (scenario == "scene-change")
                    ids.Contains(frame == 2 ? 10 : 20).ShouldBeFalse();
                fixture.RendererApi.GetError().ShouldBe(0);
            }
        }
        finally
        {
                }
    }

    private Mesh ReferenceMesh(bool lod)
    {
        var mesh = new Mesh("reference-cube");
        mesh.Vertices.AddRange(MeshFactory.CubeTemplate.Vertices);
        mesh.Indices.AddRange(MeshFactory.CubeTemplate.Indices);
        if (lod)
        {
            // Deliberately identifiable LOD: front face only; zero error forces selection.
            mesh.LodIndices.Add(MeshFactory.CubeTemplate.Indices[..6]);
            mesh.LodErrors.Add(0f);
        }
        mesh.Initialize(fixture.VertexArrayFactory, fixture.VertexBufferFactory, fixture.IndexBufferFactory);
        return mesh;
    }

    private static Context MakeScene(string scenario, int id)
    {
        var context = new Context();
        Add(context, id, new(-0.8f, 0, 0), new(1, 0.3f, 0.15f, 1));
        Add(context, id + 1, new(0.8f, 0, 0), new(1, 0.3f, 0.15f, 1));
        var floor = new Entity(id + 2, "floor");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateScale(5, 0.2f, 4) * Matrix4x4.CreateTranslation(0, -0.8f, 0));
        floor.AddComponent(transform);
        floor.AddComponent(new ModelRendererComponent { Color = new(0.4f, 0.5f, 0.6f, 1) });
        context.Register(floor);
        if (scenario != "hdri")
        {
            var sun = new Entity(100, "sun");
            sun.AddComponent(new DirectionalLightComponent { Direction = new(-1, -2, -1), Intensity = 2 });
            context.Register(sun);
        }
        if (scenario == "point-cutout")
        {
            var lamp = new Entity(101, "lamp");
            var pose = new TransformComponent();
            pose.SetWorldTransform(Matrix4x4.CreateTranslation(2, 2, 3));
            lamp.AddComponent(pose);
            lamp.AddComponent(new PointLightComponent { Range = 12, Intensity = 5, CastsShadow = true });
            context.Register(lamp);
        }
        return context;
    }

    private static void Add(Context context, int id, Vector3 position, Vector4 color)
    {
        var entity = new Entity(id, "reference");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(position));
        entity.AddComponent(transform);
        entity.AddComponent(new ModelRendererComponent
            { ModelPath = "models/reference.glb", MeshIndex = 0, FactorsSeeded = true, Color = color, Metallic = 0.5f });
        context.Register(entity);
    }

    private static unsafe void WriteHdr(string path, Engine.Renderer.Buffers.FrameBuffer.IFrameBuffer target)
    {
        var pixels = new float[Width * Height * 4];
        target.Bind();
        try
        {
            SilkNetContext.GL.ReadBuffer(GLEnum.ColorAttachment0);
            fixed (float* p = pixels)
                SilkNetContext.GL.ReadPixels(0, 0, Width, Height, PixelFormat.Rgba, PixelType.Float, p);
        }
        finally { target.Unbind(); }
        pixels.All(float.IsFinite).ShouldBeTrue($"{path}: nonfinite HDR samples");
        File.WriteAllBytes(path, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
    }

    private static unsafe int[] ReadIds(Engine.Renderer.Buffers.FrameBuffer.IFrameBuffer target)
    {
        var ids = new int[Width * Height];
        target.Bind();
        try
        {
            SilkNetContext.GL.ReadBuffer(GLEnum.ColorAttachment1);
            fixed (int* p = ids)
                SilkNetContext.GL.ReadPixels(0, 0, Width, Height, PixelFormat.RedInteger, PixelType.Int, p);
        }
        finally { target.Unbind(); }
        return ids;
    }

    private static void WriteConstantHdr(string path)
    {
        using var file = File.Create(path);
        file.Write(System.Text.Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 4 +X 8\n"));
        for (var i = 0; i < 32; i++) file.Write([128, 96, 64, 130]);
    }
}
