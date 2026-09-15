using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Scene;

public class SceneSerializerDimensionTests
{
    [Fact]
    public void Deserialize_ThreeD_SetsSceneDimension()
    {
        var path = WriteScene("""{"Scene":"t","Dimension":"ThreeD","Entities":[]}""");
        try
        {
            using var scene = CreateEmptyScene();
            Serializer().Deserialize(scene, path);
            scene.Dimension.ShouldBe(SceneDimension.ThreeD);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Deserialize_MissingDimension_LeavesDefaultTwoD()
    {
        var path = WriteScene("""{"Scene":"t","Entities":[]}""");
        try
        {
            using var scene = CreateEmptyScene();
            Serializer().Deserialize(scene, path);
            scene.Dimension.ShouldBe(SceneDimension.TwoD);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SerializeThenDeserialize_PreservesThreeD()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dim-rt-{Guid.NewGuid():N}.scene");
        var serializer = Serializer();
        try
        {
            using (var source = CreateEmptyScene())
            {
                source.Dimension = SceneDimension.ThreeD;
                serializer.Serialize(source, path);
            }

            using var loaded = CreateEmptyScene();
            loaded.Dimension.ShouldBe(SceneDimension.TwoD);
            serializer.Deserialize(loaded, path);
            loaded.Dimension.ShouldBe(SceneDimension.ThreeD);
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
        var path = Path.Combine(Path.GetTempPath(), $"dim-{Guid.NewGuid():N}.scene");
        File.WriteAllText(path, json);
        return path;
    }
}
