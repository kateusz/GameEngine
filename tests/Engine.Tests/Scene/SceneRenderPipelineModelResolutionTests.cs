using System.Numerics;
using ECS;
using Engine.Project;
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

[Collection("StaticMeshMerging")]
public class SceneRenderPipelineModelResolutionTests : IDisposable
{
    public SceneRenderPipelineModelResolutionTests() => PointShadowCache.Clear();

    public void Dispose()
    {
        PointShadowCache.Clear();
        SceneRenderPipeline.ResetMergeState();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedModel_IsResolvedOnceAcrossEntitiesAndShadowPasses(bool missing)
    {
        var context = CreateContext();
        var models = Substitute.For<IModelFactory>();
        var mesh = new Mesh("shared");
        models.Create(Arg.Any<string>()).Returns(missing ? null : new Model("shared", [mesh]));
        var graphics = Graphics();

        Render(context, graphics, models);

        graphics.GetStats().DirectionalShadow.ShouldBeTrue();
        if (missing)
            // The lamp is inside the fallback cubes: only the two cascades and color draw them.
            graphics.CubeDraws.ShouldBe(300);
        else
            // Two directional cascades, six point faces and the color pass still draw the instances.
            graphics.InstanceDrawCalls.ShouldBe(9);
        models.Received(1).Create(Arg.Any<string>());
    }

    [Fact]
    public void InvalidSubmesh_IsSkippedWithoutRepeatedModelResolution()
    {
        var context = CreateContext();
        foreach (var (_, renderer, _) in context.View<ModelRendererComponent, TransformComponent>())
            renderer.MeshIndex = 99;
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(new Model("shared", [new Mesh("shared")]));
        var graphics = Graphics();

        Render(context, graphics, models);

        graphics.MeshDraws.ShouldBe(0);
        graphics.CubeDraws.ShouldBe(0);
        models.Received(1).Create(Arg.Any<string>());
    }

    [Fact]
    public void NextRender_UsesCurrentFactoryResultInsteadOfRetainingAnOldModel()
    {
        var context = CreateContext();
        var models = Substitute.For<IModelFactory>();
        var first = new Mesh("before clear");
        var second = new Mesh("after clear");
        models.Create(Arg.Any<string>()).Returns(new Model("shared", [first]), new Model("shared", [second]));
        Render(context, Graphics(), models);
        models.Clear();
        first.Dispose();
        var nextGraphics = Graphics();

        Render(context, nextGraphics, models);

        models.Received(2).Create(Arg.Any<string>());
        nextGraphics.InstanceMeshes.ShouldNotBeEmpty();
        nextGraphics.InstanceMeshes.ShouldAllBe(mesh => ReferenceEquals(mesh, second));
    }

    [Fact]
    public void ProjectChange_ResolvesSameAssetPathAgainstNewAssetsDirectory()
    {
        var context = CreateContext();
        var models = Substitute.For<IModelFactory>();
        models.Create(Arg.Any<string>()).Returns(new Model("shared", [new Mesh("shared")]));
        var project = new ProjectContext();
        PathBuilder.UseProjectContext(project);
        var firstRoot = Path.Combine(Path.GetTempPath(), "render-project-one");
        var secondRoot = Path.Combine(Path.GetTempPath(), "render-project-two");
        project.Apply(firstRoot);
        var firstPath = PathBuilder.Resolve("shared.glb");
        Render(context, Graphics(), models);
        project.Apply(secondRoot);
        var secondPath = PathBuilder.Resolve("shared.glb");

        Render(context, Graphics(), models);

        firstPath.ShouldNotBe(secondPath);
        models.Received(1).Create(firstPath);
        models.Received(1).Create(secondPath);
    }

    private static Context CreateContext()
    {
        PathBuilder.UseProjectContext(new ProjectContext());
        var context = new Context();
        var sun = new Entity(1001, "sun");
        sun.AddComponent(new DirectionalLightComponent { Direction = new Vector3(0, -1, -1) });
        context.Register(sun);
        var lamp = new Entity(1002, "lamp");
        lamp.AddComponent(new TransformComponent());
        lamp.AddComponent(new PointLightComponent { CastsShadow = true, Range = 10 });
        context.Register(lamp);
        for (var i = 1; i <= 100; i++)
        {
            var entity = new Entity(i, "instance");
            entity.AddComponent(new TransformComponent());
            entity.AddComponent(new ModelRendererComponent { ModelPath = "shared.glb", MeshIndex = 0 });
            context.Register(entity);
        }
        return context;
    }

    private static MeshDrawRecordingGraphics3D Graphics()
    {
        var graphics = new MeshDrawRecordingGraphics3D { AcceptPointShadowFaces = true };
        graphics.SetShadowQuality(new ShadowQuality(Cascades: 2));
        return graphics;
    }

    private static void Render(Context context, MeshDrawRecordingGraphics3D graphics, IModelFactory models)
    {
        var eye = new Vector3(0, 0, 5);
        var vp = Matrix4x4.CreateLookAt(eye, Vector3.Zero, Vector3.UnitY)
                 * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, 1.5f, 0.1f, 100);
        SceneRenderPipeline.RenderScene(context, Substitute.For<IGraphics2D>(), graphics,
            Substitute.For<ITextureFactory>(), models, new SceneView(vp, eye));
    }
}
