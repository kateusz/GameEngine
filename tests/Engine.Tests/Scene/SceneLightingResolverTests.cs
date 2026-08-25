using System.Numerics;
using ECS;
using Engine.Scene;
using SceneComponents;
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

    [Fact]
    public void Resolve_PointLight_UsesWorldPositionAndColor()
    {
        var context = new Context();
        var entity = Entity.Create(1, "lamp");
        entity.AddComponent(new PointLightComponent
        {
            Color = new Vector4(1f, 0.5f, 0.25f, 1f),
            Constant = 1f,
            Linear = 0.09f,
            Quadratic = 0.032f
        });
        var transform = new TransformComponent(new Vector3(2, 3, 4), Vector3.Zero, Vector3.One);
        transform.SetWorldTransform(transform.GetTransform());
        entity.AddComponent(transform);
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.PointLights.ShouldNotBeNull();
        lighting.PointLights!.Length.ShouldBe(1);
        lighting.PointLights[0].Position.ShouldBe(new Vector3(2, 3, 4));
        lighting.PointLights[0].Color.ShouldBe(new Vector3(1f, 0.5f, 0.25f));
        lighting.PointLights[0].Constant.ShouldBe(1f);
        lighting.PointLights[0].Linear.ShouldBe(0.09f);
        lighting.PointLights[0].Quadratic.ShouldBe(0.032f);
    }

    [Fact]
    public void Resolve_FivePointLights_KeepsFirstFour()
    {
        var context = new Context();
        for (var i = 0; i < 5; i++)
        {
            var entity = Entity.Create(i + 1, $"lamp{i}");
            entity.AddComponent(new PointLightComponent
            {
                Color = new Vector4(i, 0f, 0f, 1f)
            });
            context.Register(entity);
        }

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.PointLights.ShouldNotBeNull();
        lighting.PointLights!.Length.ShouldBe(4);
        lighting.PointLights.Select(p => p.Color.X).Distinct().Count().ShouldBe(4);
        foreach (var light in lighting.PointLights)
            light.Color.X.ShouldBeInRange(0f, 4f);
    }

    [Fact]
    public void Resolve_PointLight_WithoutTransform_SitsAtOrigin()
    {
        var context = new Context();
        var entity = Entity.Create(1, "lamp");
        entity.AddComponent(new PointLightComponent());
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.PointLights![0].Position.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void Resolve_SpotLight_RotatesLocalDirectionByTransform()
    {
        var context = new Context();
        var entity = Entity.Create(1, "spot");
        entity.AddComponent(new SpotLightComponent
        {
            Direction = new Vector3(0, 0, -1),
            InnerCutoff = 12.5f,
            OuterCutoff = 17.5f
        });
        var rotation = Matrix4x4.CreateRotationY(MathF.PI / 2f);
        var transform = new TransformComponent();
        transform.SetWorldTransform(rotation);
        entity.AddComponent(transform);
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.SpotLights.ShouldNotBeNull();
        lighting.SpotLights!.Length.ShouldBe(1);
        var expected = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 0, -1), rotation));
        lighting.SpotLights[0].Direction.X.ShouldBe(expected.X, 0.0001f);
        lighting.SpotLights[0].Direction.Y.ShouldBe(expected.Y, 0.0001f);
        lighting.SpotLights[0].Direction.Z.ShouldBe(expected.Z, 0.0001f);
        lighting.SpotLights[0].InnerCutoffCos.ShouldBe(MathF.Cos(12.5f * MathF.PI / 180f), 0.0001f);
        lighting.SpotLights[0].OuterCutoffCos.ShouldBe(MathF.Cos(17.5f * MathF.PI / 180f), 0.0001f);
    }

    [Fact]
    public void Resolve_ThreeSpotLights_KeepsFirstTwo()
    {
        var context = new Context();
        for (var i = 0; i < 3; i++)
        {
            var entity = Entity.Create(i + 1, $"spot{i}");
            entity.AddComponent(new SpotLightComponent
            {
                Color = new Vector4(0f, i, 0f, 1f)
            });
            context.Register(entity);
        }

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.SpotLights.ShouldNotBeNull();
        lighting.SpotLights!.Length.ShouldBe(2);
        lighting.SpotLights.Select(s => s.Color.Y).Distinct().Count().ShouldBe(2);
        foreach (var light in lighting.SpotLights)
            light.Color.Y.ShouldBeInRange(0f, 2f);
    }

    [Fact]
    public void Resolve_SpotLight_WithoutTransform_UsesNormalizedLocalDirection()
    {
        var context = new Context();
        var entity = Entity.Create(1, "spot");
        entity.AddComponent(new SpotLightComponent { Direction = new Vector3(0, 0, -2) });
        context.Register(entity);

        var lighting = SceneLightingResolver.Resolve(context);

        lighting.SpotLights![0].Direction.ShouldBe(new Vector3(0, 0, -1));
        lighting.SpotLights[0].Position.ShouldBe(Vector3.Zero);
    }
}
