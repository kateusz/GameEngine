using Engine.Scene;
using Engine.Scene.Serializer;
using Shouldly;

namespace Engine.Tests.Scene;

public class SceneSerializerDimensionTests
{
    [Fact]
    public void PeekDimension_ThreeD_ReturnsThreeD()
    {
        var path = WriteScene("""{"Scene":"t","Dimension":"ThreeD","Entities":[]}""");
        try
        {
            var serializer = new SceneSerializer(new ComponentSerializerRegistry(), new SerializerOptions());
            serializer.PeekDimension(path).ShouldBe(SceneDimension.ThreeD);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PeekDimension_Missing_ReturnsTwoD()
    {
        var path = WriteScene("""{"Scene":"t","Entities":[]}""");
        try
        {
            var serializer = new SceneSerializer(new ComponentSerializerRegistry(), new SerializerOptions());
            serializer.PeekDimension(path).ShouldBe(SceneDimension.TwoD);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteScene(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dim-{Guid.NewGuid():N}.scene");
        File.WriteAllText(path, json);
        return path;
    }
}
