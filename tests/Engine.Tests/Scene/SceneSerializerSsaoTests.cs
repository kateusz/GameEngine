using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using SceneComponents.Camera;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Scene;

public class SceneSerializerSsaoTests
{
    [Fact]
    public void SerializeThenDeserialize_PreservesSsao()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ssao-rt-{Guid.NewGuid():N}.scene");
        var serializer = Serializer();
        try
        {
            using (var source = CreateEmptyScene())
            {
                var camera = source.CreateEntity("Camera");
                camera.AddComponent(new CameraComponent
                {
                    Primary = true,
                    Ssao = true,
                    SsaoRadius = 1.25f,
                    SsaoStrength = 0.4f
                });
                serializer.Serialize(source, path);
            }

            using var loaded = CreateEmptyScene();
            serializer.Deserialize(loaded, path);
            CameraComponent component = null!;
            foreach (var (_, camera) in loaded.Context.View<CameraComponent>())
                component = camera;
            component.Ssao.ShouldBeTrue();
            component.SsaoRadius.ShouldBe(1.25f);
            component.SsaoStrength.ShouldBe(0.4f);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Deserialize_MissingSsaoKeys_UsesDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ssao-miss-{Guid.NewGuid():N}.scene");
        var serializer = Serializer();
        try
        {
            using (var source = CreateEmptyScene())
            {
                source.CreateEntity("Camera").AddComponent(new CameraComponent { Primary = true, Ssao = true });
                serializer.Serialize(source, path);
            }

            var json = File.ReadAllText(path)
                .Replace("\"Ssao\": true,", "", StringComparison.Ordinal)
                .Replace("\"SsaoRadius\": 0.5,", "", StringComparison.Ordinal)
                .Replace("\"SsaoStrength\": 1,", "", StringComparison.Ordinal);
            File.WriteAllText(path, json);

            using var loaded = CreateEmptyScene();
            serializer.Deserialize(loaded, path);
            CameraComponent component = null!;
            foreach (var (_, camera) in loaded.Context.View<CameraComponent>())
                component = camera;
            component.Ssao.ShouldBeFalse();
            component.SsaoRadius.ShouldBe(0.5f);
            component.SsaoStrength.ShouldBe(1f);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static SceneSerializer Serializer() =>
        new(new ComponentSerializerRegistry(), new SerializerOptions());

    private static EngineScene CreateEmptyScene() =>
        new("t", new ECS.Context(),
            new SystemManager(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            null!,
            NullCameraQueries.Instance);
}
