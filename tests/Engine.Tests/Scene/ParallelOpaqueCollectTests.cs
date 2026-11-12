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
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
[Collection("SceneRenderPipeline")]
public class ParallelOpaqueCollectTests : IDisposable
{
    private readonly int _threshold;
    private readonly bool _forceSerial;

    public ParallelOpaqueCollectTests()
    {
        _threshold = SceneRenderPipeline.ParallelOpaqueCollectThreshold;
        _forceSerial = SceneRenderPipeline.ForceSerialOpaqueCollect;
        PointShadowCache.Clear();
        SceneRenderPipeline.ResetMergeState();
    }

    public void Dispose()
    {
        SceneRenderPipeline.ParallelOpaqueCollectThreshold = _threshold;
        SceneRenderPipeline.ForceSerialOpaqueCollect = _forceSerial;
        SceneRenderPipeline.ResetMergeState();
        PointShadowCache.Clear();
    }

    [Fact]
    public void ParallelCollect_MatchesSerial_CubeDrawCounts()
    {
        var context = BuildCubes(80);
        var view = new SceneView(
            Matrix4x4.CreateOrthographic(40f, 40f, 0.1f, 100f),
            PointShadows: false,
            DirectionalShadows: false);

        SceneRenderPipeline.ForceSerialOpaqueCollect = true;
        var serial = new MeshDrawRecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), serial, Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(), view);

        SceneRenderPipeline.ForceSerialOpaqueCollect = false;
        SceneRenderPipeline.ParallelOpaqueCollectThreshold = 8;
        var parallel = new MeshDrawRecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), parallel, Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(), view);

        parallel.CubeDraws.ShouldBe(serial.CubeDraws);
        parallel.CubeDraws.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void BelowThreshold_UsesSerialPath_StillDraws()
    {
        var context = BuildCubes(4);
        SceneRenderPipeline.ForceSerialOpaqueCollect = false;
        SceneRenderPipeline.ParallelOpaqueCollectThreshold = 64;
        var graphics = new MeshDrawRecordingGraphics3D();

        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics, Substitute.For<ITextureFactory>(),
            Substitute.For<IModelFactory>(),
            new SceneView(
                Matrix4x4.CreateOrthographic(40f, 40f, 0.1f, 100f),
                PointShadows: false,
                DirectionalShadows: false));

        graphics.CubeDraws.ShouldBe(4);
    }

    [Fact]
    public void ParallelCollect_CachedModel_QueuesInstances()
    {
        var mesh = CreateTriangle();
        var model = new Model("/parallel.glb", [mesh]);
        var factory = Substitute.For<IModelFactory>();
        factory.Create(Arg.Any<string>()).Returns(model);
        Model? boxed = model;
        factory.TryGetCached(Arg.Any<string>(), out Arg.Any<Model?>())
            .Returns(x =>
            {
                x[1] = boxed;
                return true;
            });

        var context = new Context();
        for (var i = 0; i < 80; i++)
        {
            var entity = new Entity(i + 1, $"m{i}");
            var transform = new TransformComponent();
            transform.SetWorldTransform(Matrix4x4.CreateTranslation(i * 0.1f, 0f, 0f));
            entity.AddComponent(transform);
            entity.AddComponent(new ModelRendererComponent
            {
                ModelPath = "/parallel.glb",
                MeshIndex = 0
            });
            context.Register(entity);
        }

        SceneRenderPipeline.ForceSerialOpaqueCollect = false;
        SceneRenderPipeline.ParallelOpaqueCollectThreshold = 8;
        var graphics = new MeshDrawRecordingGraphics3D();
        SceneRenderPipeline.RenderScene(
            context, Substitute.For<IGraphics2D>(), graphics, Substitute.For<ITextureFactory>(),
            factory,
            new SceneView(
                Matrix4x4.CreateOrthographic(40f, 40f, 0.1f, 100f),
                PointShadows: false,
                DirectionalShadows: false));

        graphics.InstanceDrawCalls.ShouldBeGreaterThan(0);
        model.Dispose();
    }

    private static Context BuildCubes(int count)
    {
        var context = new Context();
        for (var i = 0; i < count; i++)
        {
            var entity = new Entity(i + 1, $"c{i}");
            var transform = new TransformComponent();
            transform.SetWorldTransform(Matrix4x4.CreateTranslation((i % 8) * 0.25f, (i / 8) * 0.25f, 0f));
            entity.AddComponent(transform);
            entity.AddComponent(new ModelRendererComponent());
            context.Register(entity);
        }

        return context;
    }

    private static Mesh CreateTriangle()
    {
        var mesh = new Mesh("tri");
        mesh.Vertices.Add(new Mesh.Vertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(1f, 0f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(0f, 1f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Indices.AddRange([0u, 1u, 2u]);
        return mesh;
    }
}

