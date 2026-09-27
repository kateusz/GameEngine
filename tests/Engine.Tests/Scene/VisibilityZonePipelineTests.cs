using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using NSubstitute;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Scene;

public class VisibilityZonePipelineTests
{
    [Fact]
    public void Aabb_ContainsPoint_RespectsWorldTransform()
    {
        var local = new Aabb(new Vector3(-1f), new Vector3(1f));
        var world = Matrix4x4.CreateTranslation(10f, 0f, 0f);
        Aabb.ContainsPoint(new Vector3(10.5f, 0f, 0f), world, local).ShouldBeTrue();
        Aabb.ContainsPoint(new Vector3(0f, 0f, 0f), world, local).ShouldBeFalse();
    }

    [Fact]
    public void RenderScene_ZoneMemberOutsideActiveZone_SkipsMeshDraw()
    {
        var context = SceneWithSun();
        var zone = new Entity(10, "room");
        zone.AddComponent(new TransformComponent());
        zone.AddComponent(new VisibilityZoneComponent
        {
            Min = new Vector3(-5f),
            Max = new Vector3(5f)
        });
        context.Register(zone);

        var prop = new Entity(2, "prop");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(0f, 0f, 0f));
        prop.AddComponent(transform);
        prop.AddComponent(new ModelRendererComponent { ModelPath = @"C:\prop.glb", VisibilityZoneEntityId = 10 });
        context.Register(prop);

        var mesh = CreateTriangleMesh("part");
        var models = Substitute.For<Engine.Renderer.Models.IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(new Engine.Renderer.Models.Model(@"C:\prop.glb", [mesh]));

        var graphics = new MeshDrawRecordingGraphics3D();
        var eye = new Vector3(100f, 0f, 0f);
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<Engine.Renderer.Textures.ITextureFactory>(),
            models,
            new SceneView(ViewProjection(eye), eye));

        graphics.MeshDraws.ShouldBe(0);
        mesh.Dispose();
    }

    [Fact]
    public void RenderScene_ZoneMemberInsideActiveZone_DrawsMesh()
    {
        var context = SceneWithSun();
        var zone = new Entity(10, "room");
        zone.AddComponent(new TransformComponent());
        zone.AddComponent(new VisibilityZoneComponent
        {
            Min = new Vector3(-5f),
            Max = new Vector3(5f)
        });
        context.Register(zone);

        var prop = new Entity(2, "prop");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(0f, 0f, 0f));
        prop.AddComponent(transform);
        prop.AddComponent(new ModelRendererComponent { ModelPath = @"C:\prop.glb", VisibilityZoneEntityId = 10 });
        context.Register(prop);

        var mesh = CreateTriangleMesh("part");
        var models = Substitute.For<Engine.Renderer.Models.IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(new Engine.Renderer.Models.Model(@"C:\prop.glb", [mesh]));

        var graphics = new MeshDrawRecordingGraphics3D();
        var eye = Vector3.Zero;
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<Engine.Renderer.Textures.ITextureFactory>(),
            models,
            new SceneView(ViewProjection(eye), eye));

        graphics.MeshDraws.ShouldBe(2);
        mesh.Dispose();
    }

    private static Context SceneWithSun()
    {
        var context = new Context();
        var sun = new Entity(99, "sun");
        sun.AddComponent(new DirectionalLightComponent());
        context.Register(sun);
        return context;
    }

    private static Engine.Renderer.Meshes.Mesh CreateTriangleMesh(string name)
    {
        var mesh = new Engine.Renderer.Meshes.Mesh(name);
        mesh.Vertices.Add(new Engine.Renderer.Meshes.Mesh.Vertex(
            Vector3.Zero, Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Engine.Renderer.Meshes.Mesh.Vertex(
            new Vector3(1f, 0f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Engine.Renderer.Meshes.Mesh.Vertex(
            new Vector3(0f, 1f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Indices.AddRange([0u, 1u, 2u]);
        return mesh;
    }

    private static Matrix4x4 ViewProjection(Vector3 eye)
    {
        var view = Matrix4x4.CreateLookAt(eye, eye + Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 100f);
        return view * projection;
    }

}
