using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Scene;
using SceneComponents.Lighting;
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
        var entity = Entity.Create(1, "ambient");
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
        var entity = Entity.Create(1, "sun");
        entity.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0, -2, 0),
            Color = new Vector4(1f, 0.9f, 0.8f, 1f)
        });
        context.Register(entity);

        SceneRenderPipeline.ResolveDirectional(context)
            .ShouldBe((new Vector3(0, -1, 0), new Vector3(1f, 0.9f, 0.8f)));
    }

    [Fact]
    public void ResolveDirectional_MultipleLights_FirstWins()
    {
        var context = new Context();

        var first = Entity.Create(1, "first");
        first.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(1, 0, 0),
            Color = new Vector4(1f, 0f, 0f, 1f)
        });
        context.Register(first);

        var second = Entity.Create(2, "second");
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
        var entity = Entity.Create(1, "sun");
        entity.AddComponent(new DirectionalLightComponent { Direction = Vector3.Zero });
        context.Register(entity);

        SceneRenderPipeline.ResolveDirectional(context).Direction.ShouldBe(LightingMath.DefaultDirection);
    }
}
