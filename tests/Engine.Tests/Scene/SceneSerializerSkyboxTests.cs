using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Scene;

public class SceneSerializerSkyboxTests
{
    [Fact]
    public void Deserialize_Skybox_SetsPath()
    {
        var path = WriteScene("""{"Scene":"t","Skybox":"sky/day.png","Entities":[]}""");
        try
        {
            using var scene = CreateEmptyScene();
            Serializer().Deserialize(scene, path);
            scene.Skybox.ShouldBe("sky/day.png");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Deserialize_MissingSkybox_LeavesEmpty()
    {
        var path = WriteScene("""{"Scene":"t","Entities":[]}""");
        try
        {
            using var scene = CreateEmptyScene();
            scene.Skybox = "old.png";
            Serializer().Deserialize(scene, path);
            scene.Skybox.ShouldBe("");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Serialize_EmptySkybox_OmitsKey()
    {
        using var scene = CreateEmptyScene();
        var json = Serializer().SerializeToString(scene);
        json.ShouldNotContain("Skybox");
    }

    [Fact]
    public void SerializeThenDeserialize_PreservesSkybox()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sky-rt-{Guid.NewGuid():N}.scene");
        var serializer = Serializer();
        try
        {
            using (var source = CreateEmptyScene())
            {
                source.Skybox = "sky/day.png";
                serializer.Serialize(source, path);
            }

            using var loaded = CreateEmptyScene();
            loaded.Skybox = "old.png";
            serializer.Deserialize(loaded, path);
            loaded.Skybox.ShouldBe("sky/day.png");
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
        new("t", new Context(),
            new SystemManager(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            null!,
            NullCameraQueries.Instance);

    private static string WriteScene(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sky-{Guid.NewGuid():N}.scene");
        File.WriteAllText(path, json);
        return path;
    }
}
