using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using NSubstitute;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
[Collection("PointShadowCache")]
public class SceneRenderPipelineShadowTests
{
    public SceneRenderPipelineShadowTests() => PointShadowCache.Clear();
    [Fact]
    public void RenderScene_NoDirectionalLight_SkipsShadowPass()
    {
        var graphics = new RecordingGraphics3D();

        SceneRenderPipeline.RenderScene(
            new Context(),
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.ShadowPasses.ShouldBeEmpty();
        graphics.BeginScenes.ShouldBe(1);
        graphics.Shadows.ShouldBe([(Matrix4x4.Identity, false)]);
    }

    [Fact]
    public void RenderScene_DirectionalLight_DrawsCubeInBothPasses()
    {
        var context = new Context();
        var cube = new Entity(1, "cube");
        cube.AddComponent(new TransformComponent());
        cube.AddComponent(new ModelRendererComponent());
        context.Register(cube);

        var sun = new Entity(2, "sun");
        sun.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0f, -1f, 0f),
            Color = Vector4.One
        });
        context.Register(sun);

        var viewProjection = ViewProjection(new Vector3(0f, 2f, 5f));
        var direction = LightingMath.NormalizeDirection(new Vector3(0f, -1f, 0f));
        LightingMath.TryFitDirectionalShadow(viewProjection, direction, out var expected).ShouldBeTrue();

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(viewProjection));

        graphics.ShadowPasses.ShouldBe([expected]);
        graphics.EndShadowPasses.ShouldBe(1);
        graphics.CubeDraws.ShouldBe(2);
        graphics.Shadows.ShouldBe([(Matrix4x4.Identity, false), (expected, true)]);
        graphics.Order.ShouldBe(["shadow-off", "begin-shadow", "cube", "end-shadow", "shadow-on", "begin-scene", "cube"]);
    }

    [Fact]
    public void RenderScene_SecondFrameWithStaticWorld_UsesCachedPointShadow()
    {
        var context = ContextWithCastingLampAndCube();

        var graphics = new RecordingGraphics3D();
        var view = new SceneView(ViewProjection(new Vector3(0f, 2f, 5f)));
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);

        graphics.PointShadowFaces.Count.ShouldBe(6);
        graphics.CachedPointShadowUses.ShouldBeEmpty();

        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);

        graphics.PointShadowFaces.ShouldBeEmpty();
        graphics.CachedPointShadowUses.ShouldBe([2]);
        graphics.EndPointShadowFaces.ShouldBe(0);
    }

    [Fact]
    public void RenderScene_MovedCube_InvalidatesPointShadowCache()
    {
        var context = ContextWithCastingLampAndCube();
        var view = new SceneView(ViewProjection(new Vector3(0f, 2f, 5f)));
        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);
        graphics.PointShadowFaces.Count.ShouldBe(6);

        foreach (var (entity, transform) in context.View<TransformComponent>())
        {
            if (entity.Id != 1)
                continue;
            transform.SetWorldTransform(Matrix4x4.CreateTranslation(1f, 0f, 0f));
            break;
        }

        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);

        graphics.PointShadowFaces.Count.ShouldBe(6);
        graphics.CachedPointShadowUses.ShouldBeEmpty();
    }

    [Fact]
    public void RenderScene_EditViewThenPlayView_RedrawsPointShadows()
    {
        var context = ContextWithCastingLampAndCube();
        var playView = new SceneView(ViewProjection(new Vector3(0f, 2f, 5f)));
        var editView = new SceneView(ViewProjection(new Vector3(0f, 2f, 5f)), PointShadows: false);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), playView);
        graphics.PointShadowFaces.Count.ShouldBe(6);

        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), editView);
        graphics.PointShadowFaces.ShouldBeEmpty();

        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), playView);
        graphics.PointShadowFaces.Count.ShouldBe(6);
    }

    [Fact]
    public void RenderScene_FarLamp_StaysCachedUntilNear()
    {
        var context = new Context();
        var cube = new Entity(1, "cube");
        cube.AddComponent(new TransformComponent());
        cube.AddComponent(new ModelRendererComponent());
        context.Register(cube);

        var lamp = new Entity(2, "lamp");
        var lampTransform = new TransformComponent();
        lampTransform.SetWorldTransform(Matrix4x4.CreateTranslation(LightingMath.PointShadowDistance + 1f, 0f, 0f));
        lamp.AddComponent(lampTransform);
        lamp.AddComponent(new PointLightComponent { Range = 10f, CastsShadow = true });
        context.Register(lamp);

        var view = new SceneView(ViewProjection(Vector3.Zero), Vector3.Zero);
        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);
        graphics.PointShadowFaces.ShouldBeEmpty();

        foreach (var (entity, transform) in context.View<TransformComponent>())
        {
            if (entity.Id != 1)
                continue;
            transform.SetWorldTransform(Matrix4x4.CreateTranslation(1f, 0f, 0f));
            break;
        }
        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);
        graphics.PointShadowFaces.ShouldBeEmpty();

        lampTransform.SetWorldTransform(Matrix4x4.Identity);
        graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), Substitute.For<IModelFactory>(), view);
        graphics.PointShadowFaces.Count.ShouldBe(6);
    }

    private static Context ContextWithCastingLampAndCube()
    {
        var context = new Context();
        var cube = new Entity(1, "cube");
        cube.AddComponent(new TransformComponent());
        cube.AddComponent(new ModelRendererComponent());
        context.Register(cube);

        var lamp = new Entity(2, "lamp");
        lamp.AddComponent(new TransformComponent());
        lamp.AddComponent(new PointLightComponent
        {
            Range = 10f,
            Intensity = 1f,
            Color = Vector4.One,
            CastsShadow = true
        });
        context.Register(lamp);
        return context;
    }

    [Fact]
    public void RenderScene_OnePointLight_DrawsSixShadowFaces()
    {
        var context = ContextWithCastingLampAndCube();

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.PointShadowFaces.Count.ShouldBe(6);
        graphics.EndPointShadowFaces.ShouldBe(6);
        graphics.CubeDraws.ShouldBe(7);
        graphics.Order.Count(o => o == "begin-point-shadow").ShouldBe(6);
        graphics.Order[^2].ShouldBe("begin-scene");
        graphics.Order[^1].ShouldBe("cube");
    }

    [Fact]
    public void RenderScene_EditView_SkipsPointShadowFaces()
    {
        var context = new Context();
        var lamp = new Entity(1, "lamp");
        lamp.AddComponent(new TransformComponent());
        lamp.AddComponent(new PointLightComponent { Range = 10f, CastsShadow = true });
        context.Register(lamp);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f)), PointShadows: false));

        graphics.PointShadowFaces.ShouldBeEmpty();
    }

    [Fact]
    public void RenderScene_PointLight_DefaultDoesNotCast()
    {
        var context = new Context();
        var lamp = new Entity(1, "lamp");
        lamp.AddComponent(new TransformComponent());
        lamp.AddComponent(new PointLightComponent { Range = 10f });
        context.Register(lamp);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.PointShadowFaces.ShouldBeEmpty();
    }

    [Fact]
    public void RenderScene_PointLightBeyondShadowDistance_SkipsFaces()
    {
        var context = new Context();
        var lamp = new Entity(1, "lamp");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(LightingMath.PointShadowDistance + 1f, 0f, 0f));
        lamp.AddComponent(transform);
        lamp.AddComponent(new PointLightComponent { Range = 10f, CastsShadow = true });
        context.Register(lamp);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(Vector3.Zero), Vector3.Zero));

        graphics.PointShadowFaces.ShouldBeEmpty();
    }

    [Fact]
    public void RenderScene_PointLightAtNearRange_SkipsShadowFaces()
    {
        var context = new Context();
        var lamp = new Entity(1, "lamp");
        lamp.AddComponent(new TransformComponent());
        lamp.AddComponent(new PointLightComponent { Range = LightingMath.PointShadowNear });
        context.Register(lamp);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.PointShadowFaces.ShouldBeEmpty();
        graphics.EndPointShadowFaces.ShouldBe(0);
    }

    [Fact]
    public void RenderScene_FailedModel_DrawsFallbackCubeInBothPasses()
    {
        var context = new Context();
        var entity = new Entity(1, "broken");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new ModelRendererComponent { ModelPath = @"C:\missing-shadow-model.glb" });
        context.Register(entity);

        var sun = new Entity(2, "sun");
        sun.AddComponent(new DirectionalLightComponent { Color = Vector4.One });
        context.Register(sun);

        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns((Model?)null);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            models,
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.CubeDraws.ShouldBe(2);
        graphics.MeshDraws.ShouldBe(0);
    }

    [Fact]
    public void RenderScene_Cube_PassesEntityFactors()
    {
        var context = new Context();
        var entity = new Entity(1, "gold");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new ModelRendererComponent { Metallic = 1f, Roughness = 0.2f, Ao = 0.8f });
        context.Register(entity);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.CubeFactors.ShouldBe([(1f, 0.2f, 0.8f)]);
    }

    [Fact]
    public void RenderScene_MeshOverrideOff_PassesSubmeshFactors()
    {
        var mesh = new Mesh("part") { MetallicFactor = 0.3f, RoughnessFactor = 0.6f };
        var model = new Model(@"C:\prop.glb", [mesh]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        var entity = new Entity(1, "prop");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = @"C:\prop.glb",
            Metallic = 1f,
            Roughness = 0f,
            Ao = 0f,
            OverrideMaterial = false
        });
        context.Register(entity);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            models,
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.MeshFactors.ShouldBe([(0.3f, 0.6f, 1f)]);
        model.Dispose();
    }

    [Fact]
    public void RenderScene_MeshOverrideOn_PassesEntityFactors()
    {
        var mesh = new Mesh("part") { MetallicFactor = 0.3f, RoughnessFactor = 0.6f };
        var model = new Model(@"C:\prop.glb", [mesh]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        var entity = new Entity(1, "prop");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = @"C:\prop.glb",
            Metallic = 1f,
            Roughness = 0f,
            Ao = 0f,
            OverrideMaterial = true
        });
        context.Register(entity);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            models,
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.MeshFactors.ShouldBe([(1f, 0f, 0f)]);
        model.Dispose();
    }

    [Fact]
    public void RenderScene_CubeOutsideCameraAndLight_DrawsNothing()
    {
        var graphics = RenderCube(
            Matrix4x4.CreateTranslation(1000f, 0f, 0f),
            ViewProjection(new Vector3(0f, 2f, 5f)));

        graphics.CubeDraws.ShouldBe(0);
        graphics.ShadowPasses.Count.ShouldBe(1);
        graphics.Order.ShouldContain("begin-shadow");
        graphics.Order.ShouldContain("begin-scene");
        graphics.Order.ShouldNotContain("cube");
    }

    [Fact]
    public void RenderScene_CubePastShadowDistance_DrawsColorOnly()
    {
        var eye = new Vector3(0f, 2f, 5f);
        var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
        var graphics = RenderCube(Matrix4x4.CreateTranslation(eye + forward * 80f), ViewProjection(eye));

        graphics.CubeDraws.ShouldBe(1);
        graphics.Order.ShouldBe(["shadow-off", "begin-shadow", "end-shadow", "shadow-on", "begin-scene", "cube"]);
    }

    [Fact]
    public void RenderScene_CubeInsideLightOutsideCamera_DrawsShadowOnly()
    {
        var eye = new Vector3(0f, 2f, 5f);
        var viewProjection = ViewProjection(eye);
        var direction = LightingMath.NormalizeDirection(new Vector3(0f, -1f, 0f));
        LightingMath.TryFitDirectionalShadow(viewProjection, direction, out var light).ShouldBeTrue();
        Matrix4x4.Invert(light, out var inverseLight).ShouldBeTrue();

        var graphics = RenderCube(
            Matrix4x4.CreateTranslation(Unproject(inverseLight, 0.95f, 0f, 0.5f)),
            viewProjection);

        graphics.CubeDraws.ShouldBe(1);
        graphics.Order.ShouldBe(["shadow-off", "begin-shadow", "cube", "end-shadow", "shadow-on", "begin-scene"]);
    }

    [Fact]
    public void RenderScene_FarSubmesh_DrawsOnlyTheNearSubmeshInBothPasses()
    {
        var near = CreateTriangle("near", Vector3.Zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f));
        var far = CreateTriangle("far", new Vector3(1000f, 0f, 0f), new Vector3(1001f, 0f, 0f), new Vector3(1000f, 1f, 0f));
        var model = new Model(@"C:\prop.glb", [near, far]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);

        var context = SceneWithSun();
        var entity = new Entity(1, "prop");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new ModelRendererComponent { ModelPath = @"C:\prop.glb" });
        context.Register(entity);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            models,
            new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

        graphics.MeshDraws.ShouldBe(2);
        graphics.Order.ShouldBe(["shadow-off", "begin-shadow", "mesh", "end-shadow", "shadow-on", "begin-scene", "mesh"]);
        model.Dispose();
    }

    private static RecordingGraphics3D RenderCube(Matrix4x4 world, Matrix4x4 viewProjection)
    {
        var context = SceneWithSun();
        var cube = new Entity(1, "cube");
        var transform = new TransformComponent();
        transform.SetWorldTransform(world);
        cube.AddComponent(transform);
        cube.AddComponent(new ModelRendererComponent());
        context.Register(cube);

        var graphics = new RecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(viewProjection));
        return graphics;
    }

    private static Context SceneWithSun()
    {
        var context = new Context();
        var sun = new Entity(2, "sun");
        sun.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0f, -1f, 0f),
            Color = Vector4.One
        });
        context.Register(sun);
        return context;
    }

    private static Vector3 Unproject(Matrix4x4 inverse, float x, float y, float z)
    {
        var clip = Vector4.Transform(new Vector4(x, y, z, 1f), inverse);
        return new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    }

    private static Mesh CreateTriangle(string name, Vector3 a, Vector3 b, Vector3 c)
    {
        var vao = Substitute.For<IVertexArray>();
        vao.IndexBuffer.Returns(Substitute.For<IIndexBuffer>());
        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(Substitute.For<IVertexBuffer>());
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(3);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        var mesh = new Mesh(name);
        mesh.Vertices.Add(new Mesh.Vertex { Position = a });
        mesh.Vertices.Add(new Mesh.Vertex { Position = b });
        mesh.Vertices.Add(new Mesh.Vertex { Position = c });
        mesh.Indices.AddRange([0u, 1u, 2u]);
        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        return mesh;
    }

    private static Matrix4x4 ViewProjection(Vector3 eye)
    {
        var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
        var view = Matrix4x4.CreateLookAt(eye, eye + forward, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
        return view * projection;
    }

    /// <summary>
    /// NSubstitute cannot proxy <see cref="IGraphics3D.SetPointLights"/> because it takes a span.
    /// </summary>
    private sealed class RecordingGraphics3D : IGraphics3D
    {
        public List<(Matrix4x4 Matrix, bool Enabled)> Shadows { get; } = [];
        public List<Matrix4x4> ShadowPasses { get; } = [];
        public List<string> Order { get; } = [];
        public int EndShadowPasses { get; private set; }
        public int BeginScenes { get; private set; }
        public int CubeDraws { get; private set; }
        public int MeshDraws { get; private set; }
        public List<(float Metallic, float Roughness, float Ao)> CubeFactors { get; } = [];
        public List<(float Metallic, float Roughness, float Ao)> MeshFactors { get; } = [];

        public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled)
        {
            Shadows.Add((lightViewProjection, enabled));
            Order.Add(enabled ? "shadow-on" : "shadow-off");
        }

        public void BeginShadowPass(Matrix4x4 lightViewProjection)
        {
            ShadowPasses.Add(lightViewProjection);
            Order.Add("begin-shadow");
        }

        public void EndShadowPass()
        {
            EndShadowPasses++;
            Order.Add("end-shadow");
        }

        public List<Matrix4x4> PointShadowFaces { get; } = [];
        public List<int> CachedPointShadowUses { get; } = [];
        public int EndPointShadowFaces { get; private set; }

        public bool BeginPointShadowFace(
            int lightIndex, int entityId, int face,
            Matrix4x4 viewProjection, Vector3 lightPosition, float range)
        {
            PointShadowFaces.Add(viewProjection);
            Order.Add("begin-point-shadow");
            return true;
        }

        public bool UseCachedPointShadow(int lightIndex, int entityId)
        {
            CachedPointShadowUses.Add(entityId);
            Order.Add("use-cached-point-shadow");
            return true;
        }

        public void EndPointShadowFace()
        {
            EndPointShadowFaces++;
            Order.Add("end-point-shadow");
        }

        public void BeginScene(in SceneView view)
        {
            BeginScenes++;
            Order.Add("begin-scene");
        }

        public void EndScene() { }

        public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
            float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f)
        {
            CubeDraws++;
            CubeFactors.Add((metallic, roughness, ao));
            Order.Add("cube");
        }

        public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
            float metallic = 0f, float roughness = 0.5f, float ao = 1f)
        {
            MeshDraws++;
            MeshFactors.Add((metallic, roughness, ao));
            Order.Add("mesh");
        }

        public void SetAmbientLight(Vector3 color, float strength) { }
        public void SetDirectionalLight(Vector3 direction, Vector3 color) { }
        public void SetPointLights(ReadOnlySpan<PointLightData> lights) { }
        public void ResetStats() { }
        public Statistics GetStats() => new();
        public void Init() { }
        public void SetClearColor(Vector4 color) { }
        public void Clear() { }
        public void Dispose() { }
    }
}
