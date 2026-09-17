using System.Reflection;
using System.Text.Json.Nodes;
using ECS;
using Editor.Scripting;
using Engine.Scene;
using Engine.Scene.Serializer;
using Shouldly;

namespace Editor.Tests.Scripting;

public class GameAssemblyCompilerSerializerTests
{
    private readonly ComponentSerializerRegistry _registry = new();
    private readonly SerializerOptions _serializerOptions = new();

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
            var entity = new Entity(1, "e");
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
            var entity = new Entity(1, "e");
            _registry.DeserializeComponent(entity, json, _serializerOptions.Options, strict: true);

            entity.GetComponent<LocalScoreComponent>().Points.ShouldBe(99);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [SerializableComponent("ScoreComponent")]
    private sealed class LocalScoreComponent : IGameComponent
    {
        public int Points { get; set; }

        public IComponent Clone() => new LocalScoreComponent { Points = Points };
    }
}
