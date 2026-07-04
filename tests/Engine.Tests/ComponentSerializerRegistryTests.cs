using System.Text.Json.Nodes;
using ECS;
using Engine.Scene.Serializer;
using SceneComponents;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests;

public class ComponentSerializerRegistryTests
{
    private readonly ComponentSerializerRegistry _registry = new();
    private readonly SerializerOptions _serializerOptions = new();

    [Fact]
    public void BuiltinComponents_RoundTrip_ThroughRegistry()
    {
        var entity = Entity.Create(1, "player");
        entity.AddComponent(new TransformComponent());
        entity.AddComponent(new SpriteRendererComponent { TexturePath = "textures/test.png" });

        var array = new JsonArray();
        _registry.SerializeEntity(entity, array, _serializerOptions.Options);
        array.Count.ShouldBe(2);

        var loaded = Entity.Create(1, "player");
        foreach (var node in array)
            _registry.DeserializeComponent(loaded, node!.AsObject(), _serializerOptions.Options, strict: true);

        loaded.HasComponent<TransformComponent>().ShouldBeTrue();
        loaded.HasComponent<SpriteRendererComponent>().ShouldBeTrue();
        loaded.GetComponent<SpriteRendererComponent>().TexturePath.ShouldBe("textures/test.png");
    }

    [Fact]
    public void StrictDeserialize_UnknownComponent_Throws()
    {
        var entity = Entity.Create(1, "e");
        var json = JsonNode.Parse("""{"Name":"UnknownComponent","Value":1}""")!.AsObject();

        Should.Throw<InvalidSceneJsonException>(() =>
            _registry.DeserializeComponent(entity, json, _serializerOptions.Options, strict: true));
    }

    [Fact]
    public void LenientDeserialize_UnknownComponent_Skips()
    {
        var entity = Entity.Create(1, "e");
        var json = JsonNode.Parse("""{"Name":"UnknownComponent","Value":1}""")!.AsObject();

        _registry.DeserializeComponent(entity, json, _serializerOptions.Options, strict: false);
        entity.GetAllComponents().ShouldBeEmpty();
    }

    [Fact]
    public void CustomGameComponent_RoundTrip_WhenRegistered()
    {
        _registry.Register<TestScoreComponent>();

        var entity = Entity.Create(1, "player");
        entity.AddComponent(new TestScoreComponent { Points = 42 });

        var array = new JsonArray();
        _registry.SerializeEntity(entity, array, _serializerOptions.Options);

        var loaded = Entity.Create(1, "player");
        foreach (var node in array)
            _registry.DeserializeComponent(loaded, node!.AsObject(), _serializerOptions.Options, strict: true);

        loaded.GetComponent<TestScoreComponent>().Points.ShouldBe(42);
    }

    [SerializableComponent]
    private sealed class TestScoreComponent : IGameComponent
    {
        public int Points { get; set; }

        public IComponent Clone() => new TestScoreComponent { Points = Points };
    }
}
