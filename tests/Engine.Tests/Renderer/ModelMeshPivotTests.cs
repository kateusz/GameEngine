using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class ModelMeshPivotTests
{
    [Fact]
    public void ToDrawTransform_SubtractsPivotBakedIntoWorld()
    {
        var pivot = new Vector3(5f, 0f, -2f);
        var world = Matrix4x4.CreateTranslation(pivot + new Vector3(10f, 0f, 0f));

        var draw = ModelMeshPivot.ToDrawTransform(world, pivot);

        draw.Translation.ShouldBe(new Vector3(10f, 0f, 0f));
    }
}
