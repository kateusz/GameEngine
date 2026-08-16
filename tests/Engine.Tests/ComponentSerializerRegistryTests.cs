using System.Reflection;
using System.Text.Json.Nodes;
using ECS;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
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
    public void ModelRendererComponent_RoundTrip_ThroughRegistry()
    {
        var entity = Entity.Create(1, "player");
        entity.AddComponent(new ModelRendererComponent
        {
            ModelPath = "models/crate.mesh",
            Color = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f),
            MetallicOverride = 0.6f,
            RoughnessOverride = 0.3f,
            SubmeshStart = 2,
            SubmeshCount = 3
        });

        var array = new JsonArray();
        _registry.SerializeEntity(entity, array, _serializerOptions.Options);

        var loaded = Entity.Create(1, "player");
        foreach (var node in array)
            _registry.DeserializeComponent(loaded, node!.AsObject(), _serializerOptions.Options, strict: true);

        loaded.HasComponent<ModelRendererComponent>().ShouldBeTrue();
        var modelRenderer = loaded.GetComponent<ModelRendererComponent>();
        modelRenderer.ModelPath.ShouldBe("models/crate.mesh");
        modelRenderer.Color.ShouldBe(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f));
        modelRenderer.MetallicOverride.ShouldBe(0.6f);
        modelRenderer.RoughnessOverride.ShouldBe(0.3f);
        modelRenderer.SubmeshStart.ShouldBe(2);
        modelRenderer.SubmeshCount.ShouldBe(3);
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

    [Fact]
    public void RegisterFromAssembly_GameComponent_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ge-ser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "ScoreComponent.cs"), """
                using ECS;

                [SerializableComponent]
                public class ScoreComponent : IGameComponent
                {
                    public int Points { get; set; }
                    public IComponent Clone() => new ScoreComponent { Points = Points };
                }
                """);

            var outputPath = Path.Combine(Path.GetTempPath(), $"GameAssembly_{Guid.NewGuid():N}.dll");
            GameAssemblyCompiler.TryCompile(dir, outputPath, emitPdb: false, useDebugOptimization: true, out _).ShouldBeTrue();
            _registry.RegisterFromAssembly(Assembly.LoadFrom(outputPath));

            var json = JsonNode.Parse("""{"Name":"ScoreComponent","Points":7}""")!.AsObject();
            var entity = Entity.Create(1, "e");
            _registry.DeserializeComponent(entity, json, _serializerOptions.Options, strict: true);

            entity.GetAllComponents().Single().GetType().Name.ShouldBe("ScoreComponent");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void UnregisterAssembly_DoesNotRemoveSerializerOwnedByAnotherAssembly()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ge-ser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "ScoreComponent.cs"), """
                using ECS;

                [SerializableComponent]
                public class ScoreComponent : IGameComponent
                {
                    public int Points { get; set; }
                    public IComponent Clone() => new ScoreComponent { Points = Points };
                }
                """);

            var outputPath = Path.Combine(Path.GetTempPath(), $"GameAssembly_{Guid.NewGuid():N}.dll");
            GameAssemblyCompiler.TryCompile(dir, outputPath, emitPdb: false, useDebugOptimization: true, out _).ShouldBeTrue();
            var gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, GameAssemblyCompiler.AssemblyName, StringComparison.Ordinal))
                ?? Assembly.LoadFrom(outputPath);
            _registry.RegisterFromAssembly(gameAssembly);
            _registry.Register<LocalScoreComponent>("ScoreComponent");

            _registry.UnregisterAssembly(gameAssembly);

            var json = JsonNode.Parse("""{"Name":"ScoreComponent","Points":99}""")!.AsObject();
            var entity = Entity.Create(1, "e");
            _registry.DeserializeComponent(entity, json, _serializerOptions.Options, strict: true);

            entity.GetComponent<LocalScoreComponent>().Points.ShouldBe(99);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SerializeEntity_OmitsModelRendererPose()
    {
        var renderer = new ModelRendererComponent { ModelPath = "models/crate.mesh" };
        renderer.BonePalette = new[] { System.Numerics.Matrix4x4.Identity };
        renderer.SkinningWorld = System.Numerics.Matrix4x4.CreateTranslation(1, 0, 0);
        var entity = Entity.Create(1, "imported");
        entity.AddComponent(renderer);

        var array = new JsonArray();
        _registry.SerializeEntity(entity, array, _serializerOptions.Options);

        array.Count.ShouldBe(1);
        var json = array[0]!.ToJsonString();
        json.ShouldContain("ModelRendererComponent");
        json.ShouldContain("models/crate.mesh");
        json.ShouldNotContain("BonePalette");
        json.ShouldNotContain("SkinningWorld");
    }

    [Fact]
    public void SerializeEntity_StaleHotReloadType_MatchesBySerializableName()
    {
        _registry.Register<LocalScoreComponent>("ScoreComponent");

        var entity = Entity.Create(2, "Game");
        entity.AddComponent(new StaleScoreComponent { Points = 42 });

        var array = new JsonArray();
        _registry.SerializeEntity(entity, array, _serializerOptions.Options);
        array.Count.ShouldBe(1);
        array[0]!["Name"]!.GetValue<string>().ShouldBe("ScoreComponent");
        array[0]!["Points"]!.GetValue<int>().ShouldBe(42);

        var loaded = Entity.Create(2, "Game");
        _registry.DeserializeComponent(loaded, array[0]!.AsObject(), _serializerOptions.Options, strict: true);
        loaded.GetComponent<LocalScoreComponent>().Points.ShouldBe(42);
    }

    [SerializableComponent]
    private sealed class TestScoreComponent : IGameComponent
    {
        public int Points { get; set; }

        public IComponent Clone() => new TestScoreComponent { Points = Points };
    }

    [SerializableComponent("ScoreComponent")]
    private sealed class LocalScoreComponent : IGameComponent
    {
        public int Points { get; set; }

        public IComponent Clone() => new LocalScoreComponent { Points = Points };
    }

    // Simulates a previous GameAssembly's type: same serializable name, different CLR type.
    [SerializableComponent("ScoreComponent")]
    private sealed class StaleScoreComponent : IGameComponent
    {
        public int Points { get; set; }

        public IComponent Clone() => new StaleScoreComponent { Points = Points };
    }
}
