using System.Numerics;
using ECS;
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
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Scene;

[Collection("StaticMeshMerging")]
[Trait("Category", "Unit")]
public class StaticMeshMergingTests : IDisposable
{
    public void Dispose() => SceneRenderPipeline.ResetMergeState();

    [Fact]
    public void SecondIdleFrame_TwoMeshesSameKey_DrawsOneCombinedMesh()
    {
        var (meshA, meshB, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories();

        Render(context, graphics, models, factories);
        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(1);
        graphics.InstanceDrawCalls.ShouldBe(0);
        graphics.DrawnMeshes[0].VertexCount.ShouldBe(6);
        graphics.DrawnMeshes[0].GetIndexCount().ShouldBe(6);
        graphics.DrawnMeshes[0].BakeVertices.ShouldBeNull();
        graphics.DrawnMeshes[0].BakeIndices.ShouldBeNull();
        meshA.BakeVertices!.Length.ShouldBe(3);
        meshB.BakeIndices!.Length.ShouldBe(3);

        model.Dispose();
    }

    [Fact]
    public void FirstFrame_UsesInstancePathOnly()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, Factories());

        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);

        model.Dispose();
    }

    [Fact]
    public void ThirdFrame_OneWorldChanges_ReturnsBothToInstancePath()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories();

        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);
        graphics.MergedDraws.ShouldBe(1);

        foreach (var (entity, transform) in context.View<TransformComponent>())
        {
            if (entity.Id == 2)
            {
                transform.SetWorldTransform(Matrix4x4.CreateTranslation(2f, 0f, 0f));
                break;
            }
        }

        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);

        model.Dispose();
    }

    [Fact]
    public void DynamicBody_NeverMerges()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        AddBody(context, 2, RigidBodyType.Dynamic);
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories();

        Render(context, graphics, models, factories);
        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);

        model.Dispose();
    }

    [Fact]
    public void StaticBody_MayMerge()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        AddBody(context, 1, RigidBodyType.Static);
        AddBody(context, 2, RigidBodyType.Static);
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories();

        Render(context, graphics, models, factories);
        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(1);
        graphics.InstanceDrawCalls.ShouldBe(0);

        model.Dispose();
    }

    [Fact]
    public void OneMemberOutsideFrustum_StillBakesBothMembers()
    {
        var (_, _, model, models) = TwoPartModel(farSecondPart: true);
        var context = ContextWithParts(farSecondPart: true);
        var graphics = new MeshDrawRecordingGraphics3D();
        var uploaded = new List<Mesh.Vertex>();
        var factories = Factories(uploaded.AddRange);

        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(1);
        var combined = graphics.DrawnMeshes[0];
        combined.VertexCount.ShouldBe(6);
        uploaded.ShouldContain(v => v.Position.X >= 1000f);

        model.Dispose();
    }

    [Fact]
    public void DifferentTint_SplitsGroups()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        MutateRenderer(context, 1, r => r.Color = Vector4.One);
        MutateRenderer(context, 2, r => r.Color = new Vector4(1f, 0f, 0f, 1f));
        AssertStaysOnInstancePathAfterTwoIdleFrames(context, models);
        model.Dispose();
    }

    [Fact]
    public void BakedVertex_UsesWorldPositionAndSourceEntityId()
    {
        var meshA = CreateTriangle("a", Vector3.Zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f));
        var meshB = CreateTriangle("b", new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f), new Vector3(1f, 1f, 0f));
        using var model = new Model("/merged.glb", [meshA, meshB]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        AddPart(context, 1, 0, 0f);
        AddPart(context, 2, 1, 10f);

        var graphics = new MeshDrawRecordingGraphics3D();
        var uploaded = new List<Mesh.Vertex>();
        var uploadedIndices = new List<uint>();
        var factories = Factories(uploaded.AddRange, uploadedIndices.AddRange);
        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(1);
        uploaded.Count.ShouldBe(6);
        uploaded.Count(v => v.EntityId == 1).ShouldBe(3);
        uploaded.Count(v => v.EntityId == 2).ShouldBe(3);

        var moved = uploaded.Single(v =>
            v.EntityId == 2 && MathF.Abs(v.Position.X - 11f) < 1e-4f && MathF.Abs(v.Position.Y) < 1e-4f);
        MathF.Abs(moved.Position.Y).ShouldBeLessThan(1e-4f);
        uploadedIndices.ShouldBe(new uint[] { 0u, 1u, 2u, 3u, 4u, 5u });
    }

    [Fact]
    public void FailedCombinedUpload_DrawsSourcesThatFrame()
    {
        var (meshA, meshB, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var working = Factories();
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, working);
        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models,
            (new ThrowingVertexArrayFactory(), working.VertexBuffer, working.IndexBuffer));

        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);
        graphics.InstanceMeshes.ShouldContain(meshA);
        graphics.InstanceMeshes.ShouldContain(meshB);

        model.Dispose();
    }

    [Fact]
    public void DirectionalShadow_PointShadow_AndColorPasses_DrawMergedMesh()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var sun = new Entity(99, "sun");
        sun.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0.3f, -1f, 0.2f),
            Color = Vector4.One,
            Intensity = 1f
        });
        context.Register(sun);
        AddPointLight(context, new Vector3(1f, 0.5f, 3f));

        var graphics = new MeshDrawRecordingGraphics3D { AcceptPointShadowFaces = true };
        var factories = Factories();
        Render(context, graphics, models, factories);
        graphics = new MeshDrawRecordingGraphics3D { AcceptPointShadowFaces = true };
        Render(context, graphics, models, factories);

        graphics.MergedDirectionalShadowDraws.ShouldBeGreaterThan(0);
        graphics.MergedPointShadowDraws.ShouldBeGreaterThan(0);
        graphics.MergedColorDraws.ShouldBe(1);
        graphics.InstanceDrawCalls.ShouldBe(0);

        model.Dispose();
    }

    [Fact]
    public void LightInsideMergeGroup_OnlyExcludesCastersContainingLight()
    {
        var context = new Context();
        AddCube(context, 1, 0f);
        AddCube(context, 2, 2f);
        AddPointLight(context, Vector3.Zero);
        var models = Substitute.For<IModelFactory>();
        var factories = Factories();

        Render(context, new MeshDrawRecordingGraphics3D { AcceptPointShadowFaces = true }, models, factories);
        var graphics = new MeshDrawRecordingGraphics3D { AcceptPointShadowFaces = true };
        Render(context, graphics, models, factories);

        graphics.PointShadowCubeIds.ShouldContain(2);
        graphics.PointShadowCubeIds.ShouldNotContain(1);
        graphics.MergedPointShadowDraws.ShouldBe(0);
        graphics.MergedColorDraws.ShouldBe(1);
    }

    [Fact]
    public void CulledGroup_ReusesCacheWhenVisibleAgain()
    {
        var (_, _, model, models) = TwoPartModel();
        using var source = model;
        var context = ContextWithParts();
        var factories = Factories();
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);
        var combined = graphics.DrawnMeshes.Single();

        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories, View(x: 1000f));
        graphics.MergedDraws.ShouldBe(0);
        Render(context, graphics, models, factories);

        graphics.DrawnMeshes.Single().ShouldBeSameAs(combined);
        graphics.GetStats().MergedRebuilds.ShouldBe(0);
    }

    [Fact]
    public void RendererAbsentForOneFrame_UsesInstancePathOnReturn()
    {
        var (_, _, model, models) = TwoPartModel();
        using var source = model;
        var context = ContextWithParts();
        var factories = Factories();
        var graphics = new MeshDrawRecordingGraphics3D();

        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);
        graphics.MergedDraws.ShouldBe(1);
        var entity = context.GetById(2);
        context.Remove(entity.Id);
        Render(context, graphics, models, factories);
        context.Register(entity);

        graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);
        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);
    }

    [Fact]
    public void ThirdIdleFrame_CacheHit_DoesNotIncrementMergedRebuilds()
    {
        var (_, _, model, models) = TwoPartModel();
        var context = ContextWithParts();
        var factories = Factories();

        var graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);
        graphics.GetStats().MergedRebuilds.ShouldBe(1);
        Render(context, graphics, models, factories);
        graphics.GetStats().MergedRebuilds.ShouldBe(0);

        model.Dispose();
    }

    [Fact]
    public void DifferentAlphaCutoff_SplitsGroups()
    {
        var meshA = CreateTriangle("a", Vector3.Zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f));
        meshA.AlphaCutout = true;
        meshA.AlphaCutoff = 0.25f;
        var meshB = CreateTriangle("b", new Vector3(2f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(2f, 1f, 0f));
        meshB.AlphaCutout = true;
        meshB.AlphaCutoff = 0.75f;
        using var model = new Model("/merged.glb", [meshA, meshB]);
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        AddPart(context, 1, 0, 0f);
        AddPart(context, 2, 1, 0f);
        AssertStaysOnInstancePathAfterTwoIdleFrames(context, models);
    }

    [Theory]
    [InlineData(3f)]
    [InlineData(0f)]
    [InlineData(-2f)]
    public void TwoIdleCubes_BakesTilingIntoTexCoords(float tiling)
    {
        var context = new Context();
        AddCube(context, 1, 0f);
        AddCube(context, 2, 2f);
        MutateRenderer(context, 1, r => r.TilingFactor = tiling);
        MutateRenderer(context, 2, r => r.TilingFactor = tiling);

        var models = Substitute.For<IModelFactory>();
        var uploaded = new List<Mesh.Vertex>();
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories(uploaded.AddRange);

        Render(context, graphics, models, factories);
        Render(context, graphics, models, factories);

        graphics.MergedDraws.ShouldBe(1);
        var expectedTexCoords = MeshFactory.CubeTemplate.Vertices.Select(v => v.TexCoord * tiling).ToArray();
        uploaded.Select(v => v.TexCoord).ShouldBe(expectedTexCoords.Concat(expectedTexCoords));
    }

    [Fact]
    public void TwoIdleCubes_ShareCombinedMesh()
    {
        var context = new Context();
        AddCube(context, 1, 0f);
        AddCube(context, 2, 2f);
        var models = Substitute.For<IModelFactory>();
        var graphics = new MeshDrawRecordingGraphics3D();
        var factories = Factories();

        Render(context, graphics, models, factories);
        graphics.CubeDraws.ShouldBe(2);

        Render(context, graphics, models, factories);
        graphics.MergedDraws.ShouldBe(1);
        graphics.DrawnMeshes[0].VertexCount.ShouldBe(48);
    }

    private static void Render(
        Context context,
        IGraphics3D graphics,
        IModelFactory models,
        (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer) factories,
        SceneView? view = null)
    {
        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics,
            Substitute.For<ITextureFactory>(),
            models,
            view ?? View(),
            factories.VertexArray,
            factories.VertexBuffer,
            factories.IndexBuffer);
    }

    private static (Mesh A, Mesh B, Model Model, IModelFactory Factory) TwoPartModel(bool farSecondPart = false)
    {
        var meshA = CreateTriangle("a", Vector3.Zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f));
        var meshB = CreateTriangle("b",
            new Vector3(farSecondPart ? 1000f : 2f, 0f, 0f),
            new Vector3(farSecondPart ? 1001f : 3f, 0f, 0f),
            new Vector3(farSecondPart ? 1000f : 2f, 1f, 0f));
        var model = new Model("/merged.glb", [meshA, meshB]);

        var factory = Substitute.For<IModelFactory>();
        factory.Create(Arg.Any<string>()).Returns(model);
        return (meshA, meshB, model, factory);
    }

    private static Context ContextWithParts(bool farSecondPart = false)
    {
        var context = new Context();
        AddPart(context, 1, 0, 0f);
        AddPart(context, 2, 1, farSecondPart ? 0f : 1f);
        return context;
    }

    private static void AddPart(Context context, int id, int meshIndex, float x)
    {
        var entity = new Entity(id, $"part{id}");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(x, 0f, 0f));
        entity.AddComponent(transform);
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = "/merged.glb",
            MeshIndex = meshIndex
        });
        context.Register(entity);
    }

    private static void AddCube(Context context, int id, float x)
    {
        var entity = new Entity(id, $"cube{id}");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(x, 0f, 0f));
        entity.AddComponent(transform);
        entity.AddComponent(new ModelRendererComponent());
        context.Register(entity);
    }

    private static void AddPointLight(Context context, Vector3 position)
    {
        var light = new Entity(98, "lamp");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(position));
        light.AddComponent(transform);
        light.AddComponent(new PointLightComponent { CastsShadow = true });
        context.Register(light);
    }

    private static void AddBody(Context context, int entityId, RigidBodyType bodyType)
    {
        foreach (var (entity, _) in context.View<ModelRendererComponent>())
        {
            if (entity.Id == entityId)
            {
                entity.AddComponent(new RigidBody2DComponent { BodyType = bodyType });
                return;
            }
        }
    }

    private static void MutateRenderer(Context context, int entityId, Action<ModelRendererComponent> mutate)
    {
        foreach (var (entity, renderer) in context.View<ModelRendererComponent>())
        {
            if (entity.Id != entityId)
                continue;

            mutate(renderer);
            return;
        }
    }

    private static void AssertStaysOnInstancePathAfterTwoIdleFrames(Context context, IModelFactory models)
    {
        var factories = Factories();
        Render(context, new MeshDrawRecordingGraphics3D(), models, factories);
        var graphics = new MeshDrawRecordingGraphics3D();
        Render(context, graphics, models, factories);
        graphics.MergedDraws.ShouldBe(0);
        graphics.InstanceDrawCalls.ShouldBe(2);
    }

    private static Mesh CreateTriangle(string name, Vector3 a, Vector3 b, Vector3 c)
    {
        var (vertexArray, vertexBuffer, indexBuffer) = Factories();
        var mesh = new Mesh(name);
        mesh.Vertices.Add(new Mesh.Vertex(a, Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Mesh.Vertex(b, Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Vertices.Add(new Mesh.Vertex(c, Vector3.UnitZ, Vector2.Zero, Vector3.UnitX, Vector3.UnitY));
        mesh.Indices.AddRange([0u, 1u, 2u]);
        mesh.Initialize(vertexArray, vertexBuffer, indexBuffer);
        return mesh;
    }

    private static (IVertexArrayFactory VertexArray, IVertexBufferFactory VertexBuffer, IIndexBufferFactory IndexBuffer)
        Factories(Action<List<Mesh.Vertex>>? onVertices = null, Action<uint[]>? onIndices = null)
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
        vertexBuffers
            .Create(Arg.Any<List<Mesh.Vertex>>())
            .Returns(ci =>
            {
                onVertices?.Invoke(ci.Arg<List<Mesh.Vertex>>());
                return Substitute.For<IVertexBuffer>();
            });

        var indexBuffers = Substitute.For<IIndexBufferFactory>();
        indexBuffers.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(call =>
        {
            onIndices?.Invoke(call.Arg<uint[]>());
            var indexBuffer = Substitute.For<IIndexBuffer>();
            indexBuffer.Count.Returns(call.Arg<int>());
            return indexBuffer;
        });

        return (vertexArrays, vertexBuffers, indexBuffers);
    }

    private static SceneView View(float x = 0f)
    {
        var eye = new Vector3(x, 2f, 5f);
        var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
        var view = Matrix4x4.CreateLookAt(eye, eye + forward, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
        return new SceneView(view * projection, eye);
    }

    private sealed class ThrowingVertexArrayFactory : IVertexArrayFactory
    {
        public IVertexArray Create() => throw new InvalidOperationException("upload failed");
    }
}
