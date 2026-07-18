using ECS;
using Engine.Scene.Serializer;
using SceneComponents.Lighting;
using Shouldly;

namespace Engine.Tests;

public class EnvironmentLightComponentTests
{
    [Fact]
    public void Clone_ShouldCopyHdrPathAndExposure()
    {
        var original = new EnvironmentLightComponent { HdrPath = "env/studio.hdr", Exposure = 2.5f };
        var clone = (EnvironmentLightComponent)original.Clone();
        clone.HdrPath.ShouldBe("env/studio.hdr");
        clone.Exposure.ShouldBe(2.5f);
    }

    [Fact]
    public void RoundTrip_ThroughRegistry()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();

        var entity = Entity.Create(1, "env");
        entity.AddComponent(new EnvironmentLightComponent { HdrPath = "env/sky.hdr", Exposure = 0.75f });

        var array = new System.Text.Json.Nodes.JsonArray();
        registry.SerializeEntity(entity, array, options.Options);

        var loaded = Entity.Create(1, "env");
        foreach (var node in array)
            registry.DeserializeComponent(loaded, node!.AsObject(), options.Options, strict: true);

        var env = loaded.GetComponent<EnvironmentLightComponent>();
        env.HdrPath.ShouldBe("env/sky.hdr");
        env.Exposure.ShouldBe(0.75f);
    }
}
