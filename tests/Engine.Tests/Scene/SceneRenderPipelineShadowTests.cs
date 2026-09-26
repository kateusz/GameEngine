using System.Numerics;
using ECS;
using Engine.Renderer;
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
public class SceneRenderPipelineShadowTests
{
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

        public void BeginScene(in SceneView view)
        {
            BeginScenes++;
            Order.Add("begin-scene");
        }

        public void EndScene() { }

        public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
            float tilingFactor = 1.0f)
        {
            CubeDraws++;
            Order.Add("cube");
        }

        public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1)
        {
            MeshDraws++;
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
