using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Scene;

public class ScenePostProcessSerializationTests
{
    private static EngineScene CreateScene()
    {
        var systemManager = Substitute.For<ISystemManager>();
        return new EngineScene("test-scene", "test-scene", new Context(),
            systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);
    }

    [Fact]
    public void RoundTrip_PreservesPostProcessSettings()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var serializer = new SceneSerializer(registry, options);
        var path = Path.Combine(Path.GetTempPath(), $"post-{Guid.NewGuid():N}.scene");

        var expected = new ScenePostProcessSettings(
            Exposure: 2.2f,
            BloomEnabled: false,
            BloomThreshold: 0.75f,
            BloomIntensity: 1.5f);

        try
        {
            using (var scene = CreateScene())
            {
                scene.PostProcess = expected;
                serializer.Serialize(scene, path);
            }

            using var loaded = CreateScene();
            serializer.Deserialize(loaded, path);

            loaded.PostProcess.ShouldBe(expected);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Deserialize_WithoutPostProcessKey_UsesDefaults()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var serializer = new SceneSerializer(registry, options);
        var path = Path.Combine(Path.GetTempPath(), $"post-missing-{Guid.NewGuid():N}.scene");

        try
        {
            File.WriteAllText(path,
                """
                {
                  "Scene": "legacy",
                  "BackgroundColor": [0.1, 0.1, 0.1, 1],
                  "Dimension": "TwoD",
                  "Entities": []
                }
                """);

            using var loaded = CreateScene();
            serializer.Deserialize(loaded, path);

            loaded.PostProcess.ShouldBe(new ScenePostProcessSettings());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
