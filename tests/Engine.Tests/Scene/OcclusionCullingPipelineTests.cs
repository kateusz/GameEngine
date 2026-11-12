using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Project;
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

[Collection("PathBuilder")]
[Trait("Category", "Unit")]
public class OcclusionCullingPipelineTests : IDisposable
{
    public OcclusionCullingPipelineTests()
    {
        var project = Substitute.For<IProjectContext>();
        project.AssetsPath.Returns(Path.GetTempPath());
        PathBuilder.UseProjectContext(project);
    }

    public void Dispose()
    {
        SceneRenderPipeline.ResetMergeState();
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
    }

    [Fact]
    public void RenderScene_LargeFrontMesh_CullsSmallMeshBehindAndCounts()
    {
        var (wall, prop, models) = WallAndProp();
        var context = SceneWithSun(wallAtZ: 0f, propAtZ: -2f);
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, DirectionalShadows: false);

        graphics.GetStats().OcclusionCulled.ShouldBeGreaterThan(0);
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, prop)).ShouldBe(0);
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, wall)).ShouldBe(1);

        wall.Dispose();
        prop.Dispose();
    }

    [Fact]
    public void RenderScene_WithDirectionalShadows_StillDrawsCulledMeshInShadowPass()
    {
        var (wall, prop, models) = WallAndProp();
        var context = SceneWithSun(wallAtZ: 0f, propAtZ: -2f);
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, DirectionalShadows: true);

        graphics.GetStats().OcclusionCulled.ShouldBeGreaterThan(0);
        // Color culls the prop; shadow pass still submits it.
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, prop)).ShouldBeGreaterThan(0);

        wall.Dispose();
        prop.Dispose();
    }

    [Fact]
    public void RenderScene_AlphaCutoutOccluder_DoesNotHideMeshBehind()
    {
        var (wall, prop, models) = WallAndProp(alphaCutout: true);
        var context = SceneWithSun(wallAtZ: 0f, propAtZ: -2f);
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, DirectionalShadows: false);

        graphics.GetStats().OcclusionCulled.ShouldBe(0);
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, prop)).ShouldBe(1);

        wall.Dispose();
        prop.Dispose();
    }

    [Fact]
    public void RenderScene_CameraYawed180_StillOcclusionCulls()
    {
        var (wall, prop, models) = WallAndProp();
        // Place scene along -Z from an eye that looks back toward +Z after 180° yaw.
        var context = SceneWithSun(wallAtZ: 0f, propAtZ: 2f);
        var graphics = new MeshDrawRecordingGraphics3D();

        RenderCore(
            context, graphics, models, factories: null, DirectionalShadows: false,
            eyeOverride: new Vector3(0f, 0f, -8f), yawRadians: MathF.PI);

        graphics.GetStats().OcclusionCulled.ShouldBeGreaterThan(0);
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, prop)).ShouldBe(0);

        wall.Dispose();
        prop.Dispose();
    }

    [Fact]
    public void RenderScene_OccluderAtLod1_DoesNotFalseCullWithLod0Silhouette()
    {
        var factories = Factories();
        // LOD0 covers the view; LOD1 is a tiny triangle that does not. Far camera selects LOD1.
        var wall = new Mesh("wall");
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(-40f, -40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(40f, -40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(0f, 40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(-0.01f, -0.01f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(0.01f, -0.01f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(0f, 0.01f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Indices.AddRange([0u, 1u, 2u]);
        wall.LodIndices.Add([3u, 4u, 5u]);
        wall.LodErrors.Add(0.01f);
        wall.Initialize(factories.VertexArray, factories.VertexBuffer, factories.IndexBuffer);
        wall.BakeIndicesAt(1).ShouldNotBeNull();

        var prop = CreateSmallProp(factories);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Is<string>(p => p.Contains("wall", StringComparison.OrdinalIgnoreCase)))
            .Returns(new Model("/wall.glb", [wall]));
        models.Create(Arg.Is<string>(p => p.Contains("prop", StringComparison.OrdinalIgnoreCase)))
            .Returns(new Model("/prop.glb", [prop]));

        var context = SceneWithSun(wallAtZ: 0f, propAtZ: -2f);
        var graphics = new MeshDrawRecordingGraphics3D();
        // Viewport/frustum values that make MeshLod pick level 1 at eye distance ~8.
        RenderCore(
            context, graphics, models, factories: null, DirectionalShadows: false,
            viewportHeight: 100f, frustumHeightAtUnit: 2f);

        MeshLod.Select(wall, Matrix4x4.Identity, new Vector3(0f, 0f, 8f),
            new SceneView(Matrix4x4.Identity, ViewportHeight: 100f, FrustumHeightAtUnit: 2f, Aspect: 16f / 9f), 1f)
            .ShouldBe(1);

        graphics.GetStats().OcclusionCulled.ShouldBe(0);
        graphics.InstanceMeshes.Count(m => ReferenceEquals(m, prop)).ShouldBe(1);

        wall.Dispose();
        prop.Dispose();
    }

    [Fact]
    public void RenderScene_CubeWithBrokenTexture_StillDrawsFallbackAndCanOcclude()
    {
        var factories = Factories();
        var prop = CreateSmallProp(factories);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(new Model("/prop.glb", [prop]));

        var context = new Context();
        AddSun(context);
        // Large textured cube in front; texture load fails → must still DrawCube (fallback).
        var cube = new Entity(1, "cube");
        var cubeTransform = new TransformComponent();
        cubeTransform.SetWorldTransform(
            Matrix4x4.CreateScale(40f, 40f, 1f) * Matrix4x4.CreateTranslation(0f, 0f, 0f));
        cube.AddComponent(cubeTransform);
        cube.AddComponent(new ModelRendererComponent { TexturePath = "/missing.png" });
        context.Register(cube);
        AddMeshEntity(context, 2, "prop", "/prop.glb", -2f);

        var textures = Substitute.For<ITextureFactory>();
        textures.Create(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => throw new InvalidOperationException("missing texture"));

        var graphics = new MeshDrawRecordingGraphics3D();
        RenderCore(context, graphics, models, factories: null, DirectionalShadows: false, textures: textures);

        // Finding #3: failed texture must still submit a solid cube (CPU/GPU agreement).
        graphics.CubeDraws.ShouldBeGreaterThan(0);

        prop.Dispose();
    }

    [Fact]
    public void RenderScene_MergedGroupBehindOccluder_SkipsGroupWithoutRebuild()
    {
        var factories = MergeFactories();
        var (wall, partA, partB, models) = WallAndTwoParts();
        var context = MergedGroupScene(includeWall: false);

        // Two idle frames build the combined mesh while the group is visible.
        RenderMerged(context, new MeshDrawRecordingGraphics3D(), models, factories, DirectionalShadows: false);
        var built = new MeshDrawRecordingGraphics3D();
        RenderMerged(context, built, models, factories, DirectionalShadows: false);
        built.MergedColorDraws.ShouldBe(1);
        var rebuildsAfterBuild = built.GetStats().MergedRebuilds;

        // Third frame: wall in front; group stays cached and is occlusion-culled.
        AddWallEntity(context);
        var occluded = new MeshDrawRecordingGraphics3D();
        RenderMerged(context, occluded, models, factories, DirectionalShadows: false);

        occluded.GetStats().OcclusionCulled.ShouldBeGreaterThan(0);
        occluded.MergedColorDraws.ShouldBe(0);
        occluded.GetStats().MergedRebuilds.ShouldBe(0);
        // Cache must still be valid — rebuilds on this frame stay 0 (not rebuilt).
        rebuildsAfterBuild.ShouldBeGreaterThan(0);

        wall.Dispose();
        partA.Dispose();
        partB.Dispose();
    }

    [Fact]
    public void RenderScene_MergedGroupExtendingInFront_KeepsDraw()
    {
        var factories = MergeFactories();
        var (wall, partA, partB, models) = WallAndTwoParts();
        // Parts straddle the wall: one behind, one in front → group box not fully occluded.
        var context = new Context();
        AddSun(context);
        AddPartEntity(context, 1, 0, new Vector3(-1f, 0f, 2f));
        AddPartEntity(context, 2, 1, new Vector3(1f, 0f, -2f));
        AddWallEntity(context);

        RenderMerged(context, new MeshDrawRecordingGraphics3D(), models, factories, DirectionalShadows: false);
        var graphics = new MeshDrawRecordingGraphics3D();
        RenderMerged(context, graphics, models, factories, DirectionalShadows: false);

        graphics.MergedColorDraws.ShouldBe(1);

        wall.Dispose();
        partA.Dispose();
        partB.Dispose();
    }

    private static void Render(
        Context context,
        MeshDrawRecordingGraphics3D graphics,
        IModelFactory models,
        bool DirectionalShadows) =>
        RenderCore(context, graphics, models, null, DirectionalShadows);

    private static void RenderMerged(
        Context context,
        MeshDrawRecordingGraphics3D graphics,
        IModelFactory models,
        (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer) factories,
        bool DirectionalShadows) =>
        RenderCore(context, graphics, models, factories, DirectionalShadows);

    private static void RenderCore(
        Context context,
        MeshDrawRecordingGraphics3D graphics,
        IModelFactory models,
        (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer)? factories,
        bool DirectionalShadows,
        Vector3? eyeOverride = null,
        float yawRadians = 0f,
        ITextureFactory? textures = null,
        float viewportHeight = 0f,
        float frustumHeightAtUnit = 0f)
    {
        var eye = eyeOverride ?? new Vector3(0f, 0f, 8f);
        var target = Vector3.Transform(Vector3.UnitZ * -1f, Matrix4x4.CreateRotationY(yawRadians));
        // Default look: eye → origin. With yaw, rotate the forward around Y.
        var lookTarget = yawRadians == 0f ? Vector3.Zero : eye + target;
        var view = Matrix4x4.CreateLookAt(eye, lookTarget, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 100f);
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            textures ?? Substitute.For<ITextureFactory>(),
            models,
            new SceneView(
                view * projection,
                eye,
                PointShadows: false,
                DirectionalShadows: DirectionalShadows,
                ViewportHeight: viewportHeight,
                FrustumHeightAtUnit: frustumHeightAtUnit,
                Aspect: 16f / 9f),
            factories?.VertexArray,
            factories?.VertexBuffer,
            factories?.IndexBuffer);
    }

    private static Context SceneWithSun(float wallAtZ, float propAtZ)
    {
        var context = new Context();
        AddSun(context);
        AddMeshEntity(context, 1, "wall", "/wall.glb", wallAtZ);
        AddMeshEntity(context, 2, "prop", "/prop.glb", propAtZ);
        return context;
    }

    private static Context MergedGroupScene(bool includeWall)
    {
        var context = new Context();
        AddSun(context);
        AddPartEntity(context, 1, 0, new Vector3(-0.5f, 0f, -2f));
        AddPartEntity(context, 2, 1, new Vector3(0.5f, 0f, -2f));
        if (includeWall)
            AddWallEntity(context);
        return context;
    }

    private static void AddSun(Context context)
    {
        var sun = new Entity(99, "sun");
        sun.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0.2f, -1f, 0.1f),
            Color = Vector4.One
        });
        context.Register(sun);
    }

    private static void AddMeshEntity(Context context, int id, string name, string path, float z)
    {
        var entity = new Entity(id, name);
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(0f, 0f, z));
        entity.AddComponent(transform);
        entity.AddComponent(new ModelRendererComponent { ModelPath = path });
        context.Register(entity);
    }

    private static void AddWallEntity(Context context)
    {
        var entity = new Entity(10, "wall");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.Identity);
        entity.AddComponent(transform);
        // Distinct tint so the wall never joins the parts' merge key.
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = "/wall.glb",
            Color = new Vector4(1f, 0f, 0f, 1f)
        });
        context.Register(entity);
    }

    private static void AddPartEntity(Context context, int id, int meshIndex, Vector3 position)
    {
        var entity = new Entity(id, $"part{id}");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(position));
        entity.AddComponent(transform);
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = "/parts.glb",
            MeshIndex = meshIndex
        });
        context.Register(entity);
    }

    private static (Mesh Wall, Mesh Prop, IModelFactory Models) WallAndProp(bool alphaCutout = false)
    {
        var factories = Factories();
        var wall = CreateCoveringWall(factories, alphaCutout);
        var prop = CreateSmallProp(factories);

        var wallModel = new Model("/wall.glb", [wall]);
        var propModel = new Model("/prop.glb", [prop]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Is<string>(p => p.Contains("wall", StringComparison.OrdinalIgnoreCase)))
            .Returns(wallModel);
        models.Create(Arg.Is<string>(p => p.Contains("prop", StringComparison.OrdinalIgnoreCase)))
            .Returns(propModel);
        return (wall, prop, models);
    }

    private static (Mesh Wall, Mesh PartA, Mesh PartB, IModelFactory Models) WallAndTwoParts()
    {
        var factories = Factories();
        var wall = CreateCoveringWall(factories, alphaCutout: false);
        var partA = CreateSmallProp(factories, "a");
        var partB = CreateSmallProp(factories, "b");
        var wallModel = new Model("/wall.glb", [wall]);
        var partsModel = new Model("/parts.glb", [partA, partB]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Is<string>(p => p.Contains("wall", StringComparison.OrdinalIgnoreCase)))
            .Returns(wallModel);
        models.Create(Arg.Is<string>(p => p.Contains("parts", StringComparison.OrdinalIgnoreCase)))
            .Returns(partsModel);
        return (wall, partA, partB, models);
    }

    private static Mesh CreateCoveringWall(
        (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer) factories,
        bool alphaCutout)
    {
        // One large triangle covering the view (a quad diagonal leaves center cells uncovered).
        var wall = new Mesh("wall");
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(-40f, -40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(40f, -40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Vertices.Add(new Mesh.Vertex(new Vector3(0f, 40f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        wall.Indices.AddRange([0u, 1u, 2u]);
        wall.AlphaCutout = alphaCutout;
        wall.Initialize(factories.VertexArray, factories.VertexBuffer, factories.IndexBuffer);
        return wall;
    }

    private static Mesh CreateSmallProp(
        (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer) factories,
        string name = "prop")
    {
        var prop = new Mesh(name);
        prop.Vertices.Add(new Mesh.Vertex(new Vector3(-0.1f, -0.1f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        prop.Vertices.Add(new Mesh.Vertex(new Vector3(0.1f, -0.1f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        prop.Vertices.Add(new Mesh.Vertex(new Vector3(0f, 0.1f, 0f), Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        prop.Indices.AddRange([0u, 1u, 2u]);
        prop.Initialize(factories.VertexArray, factories.VertexBuffer, factories.IndexBuffer);
        return prop;
    }

    private static (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer)
        Factories() => MergeFactories();

    private static (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer)
        MergeFactories()
    {
        var vertexArrays = Substitute.For<IVertexArrayFactory>();
        vertexArrays.Create().Returns(_ =>
        {
            var vertexArray = Substitute.For<IVertexArray>();
            IIndexBuffer indexBuffer = null!;
            vertexArray.IndexBuffer.Returns(_ => indexBuffer);
            vertexArray.When(array => array.SetIndexBuffer(Arg.Any<IIndexBuffer>()))
                .Do(call => indexBuffer = call.Arg<IIndexBuffer>());
            return vertexArray;
        });

        var vertexBuffers = Substitute.For<IVertexBufferFactory>();
        vertexBuffers.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(_ => Substitute.For<IVertexBuffer>());

        var indexBuffers = Substitute.For<IIndexBufferFactory>();
        indexBuffers.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(call =>
        {
            var indexBuffer = Substitute.For<IIndexBuffer>();
            indexBuffer.Count.Returns(call.Arg<int>());
            return indexBuffer;
        });

        return (vertexArrays, vertexBuffers, indexBuffers);
    }
}
