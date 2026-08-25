using System.Numerics;
using ECS;
using Engine.Scene;
using SceneComponents.Lighting;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
public class SceneLightingResolverTests
{
    [Fact]
    public void Resolve_NoLights_ReturnsDefaults()
    {
        var context = new Context();

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.ShouldBe(SceneLighting.Default);
    }

    [Fact]
    public void Resolve_AmbientLight_UsesComponentValues()
    {
        var context = new Context();
        var entity = Entity.Create(1, "ambient");
        entity.AddComponent(new AmbientLightComponent
        {
            Color = new Vector4(0.2f, 0.3f, 0.4f, 1f),
            Strength = 0.5f
        });
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.AmbientColor.ShouldBe(new Vector3(0.2f, 0.3f, 0.4f));
        lighting.AmbientStrength.ShouldBe(0.5f);
        lighting.DirectionalDirection.ShouldBe(SceneLighting.Default.DirectionalDirection);
        lighting.DirectionalColor.ShouldBe(SceneLighting.Default.DirectionalColor);
    }

    [Fact]
    public void Resolve_DirectionalLight_UsesComponentValues()
    {
        var context = new Context();
        var entity = Entity.Create(1, "sun");
        entity.AddComponent(new DirectionalLightComponent
        {
            Direction = new Vector3(0, -2, 0),
            Color = new Vector4(1f, 0.9f, 0.8f, 1f)
        });
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.DirectionalDirection.ShouldBe(new Vector3(0, -1, 0));
        lighting.DirectionalColor.ShouldBe(new Vector3(1f, 0.9f, 0.8f));
        lighting.AmbientColor.ShouldBe(SceneLighting.Default.AmbientColor);
        lighting.AmbientStrength.ShouldBe(SceneLighting.Default.AmbientStrength);
    }

    [Fact]
    public void Resolve_MultipleDirectionalLights_FirstWins()
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

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.DirectionalDirection.ShouldBe(new Vector3(1, 0, 0));
        lighting.DirectionalColor.ShouldBe(new Vector3(1f, 0f, 0f));
    }

    [Fact]
    public void Resolve_ZeroLengthDirection_FallsBackToDown()
    {
        var context = new Context();
        var entity = Entity.Create(1, "sun");
        entity.AddComponent(new DirectionalLightComponent { Direction = Vector3.Zero });
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.DirectionalDirection.ShouldBe(new Vector3(0, -1, 0));
    }
}
