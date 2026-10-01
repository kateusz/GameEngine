using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
public class SceneRenderPipelineLightingTests
{
    [Fact]
    public void ResolveAmbient_NoLights_ReturnsDefaults()
    {
        SceneRenderPipeline.ResolveAmbient(new Context()).ShouldBe((Vector3.One, 0.1f));
    }

    [Fact]
    public void ResolveAmbient_UsesComponentValues()
    {
        var context = new Context();
        var entity = new Entity(1, "ambient");
        entity.AddComponent(new AmbientLightComponent
        {
            Color = new Vector4(0.2f, 0.3f, 0.4f, 1f),
            Strength = 0.5f
        });
        context.Register(entity);

        SceneRenderPipeline.ResolveAmbient(context).ShouldBe((new Vector3(0.2f, 0.3f, 0.4f), 0.5f));
    }

    [Fact]
    public void ResolveDirectional_NoLights_ReturnsDefaults()
    {
        SceneRenderPipeline.ResolveDirectional(new Context())
            .ShouldBe((LightingMath.DefaultDirection, Vector3.Zero));
    }

    [Fact]
    public void ResolveDirectional_UsesComponentValues()
    {
        var context = new Context();
        var entity = new Entity(1, "sun");
        entity.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0, -2, 0),
            Color = new Vector4(1f, 0.9f, 0.8f, 1f),
            Intensity = 2f
        });
        context.Register(entity);

        SceneRenderPipeline.ResolveDirectional(context)
            .ShouldBe((new Vector3(0, -1, 0), new Vector3(2f, 1.8f, 1.6f)));
    }

    [Fact]
    public void ResolveDirectional_NegativeIntensity_ClampsToZero()
    {
        var context = new Context();
        var entity = new Entity(1, "sun");
        entity.AddComponent(new DirectionalLightComponent
        {
            Color = Vector4.One,
            Intensity = -1f
        });
        context.Register(entity);

        SceneRenderPipeline.ResolveDirectional(context).Color.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void ResolveDirectional_MultipleLights_FirstWins()
    {
        var context = new Context();

        var first = new Entity(1, "first");
        first.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(1, 0, 0),
            Color = new Vector4(1f, 0f, 0f, 1f)
        });
        context.Register(first);

        var second = new Entity(2, "second");
        second.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0, 1, 0),
            Color = new Vector4(0f, 1f, 0f, 1f)
        });
        context.Register(second);

        SceneRenderPipeline.ResolveDirectional(context)
            .ShouldBe((new Vector3(1, 0, 0), new Vector3(1f, 0f, 0f)));
    }

    [Fact]
    public void ResolveDirectional_ZeroLength_FallsBackToDown()
    {
        var context = new Context();
        var entity = new Entity(1, "sun");
        entity.AddComponent(new DirectionalLightComponent { Direction = Vector3.Zero });
        context.Register(entity);

        SceneRenderPipeline.ResolveDirectional(context).Direction.ShouldBe(LightingMath.DefaultDirection);
    }

    [Fact]
    public void AdoptSubmeshFactors_DefaultChild_CopiesMeshOnce()
    {
        var renderer = new ModelRendererComponent { MeshIndex = 0 };
        var mesh = new Mesh("Cube.008") { MetallicFactor = 0f, RoughnessFactor = 0.122727275f };

        SceneRenderPipeline.AdoptSubmeshFactors(renderer, mesh);
        renderer.Roughness.ShouldBe(0.122727275f);
        renderer.FactorsSeeded.ShouldBeTrue();

        renderer.Roughness = 0.5f;
        SceneRenderPipeline.AdoptSubmeshFactors(renderer, mesh);
        renderer.Roughness.ShouldBe(0.5f);
        mesh.Dispose();
    }

    [Fact]
    public void AdoptSubmeshFactors_EditedChild_KeepsComponentValues()
    {
        var renderer = new ModelRendererComponent { MeshIndex = 0, Metallic = 0.8f, Roughness = 0.25f };
        var mesh = new Mesh("Cube.008") { MetallicFactor = 0f, RoughnessFactor = 0.122727275f };

        SceneRenderPipeline.AdoptSubmeshFactors(renderer, mesh);

        renderer.Metallic.ShouldBe(0.8f);
        renderer.Roughness.ShouldBe(0.25f);
        renderer.FactorsSeeded.ShouldBeTrue();
        mesh.Dispose();
    }

    [Fact]
    public void ResolvePbr_UsesComponentFactors()
    {
        var renderer = new ModelRendererComponent { Metallic = 0.8f, Roughness = 0.25f, Ao = 0.4f };

        SceneRenderPipeline.ResolvePbr(renderer)
            .ShouldBe(new SceneRenderPipeline.PbrFactors(0.8f, 0.25f, 0.4f));
    }

    [Fact]
    public void ResolvePbr_OutOfRange_Clamps()
    {
        var renderer = new ModelRendererComponent { Metallic = 2f, Roughness = -1f, Ao = 3f };

        SceneRenderPipeline.ResolvePbr(renderer)
            .ShouldBe(new SceneRenderPipeline.PbrFactors(1f, 0f, 1f));
    }

    [Fact]
    public void ResolvePointLights_ApplyOffset_AddsOffsetToWorldTranslation()
    {
        var context = new Context();
        var entity = new Entity(1, "lamp");
        var transform = new TransformComponent();
        transform.SetWorldTransform(Matrix4x4.CreateTranslation(1f, 2f, 3f));
        entity.AddComponent(transform);
        entity.AddComponent(new PointLightComponent
        {
            Range = 10f,
            ApplyOffset = true,
            Offset = new Vector3(9f, 8f, 7f)
        });
        context.Register(entity);

        var lights = new PointLightData[LightingMath.MaxPointLights];
        var count = SceneRenderPipeline.ResolvePointLights(context, lights);

        count.ShouldBe(1);
        lights[0].Position.ShouldBe(new Vector3(10f, 10f, 10f));
    }
}
