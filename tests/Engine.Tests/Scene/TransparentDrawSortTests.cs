using System.Numerics;
using Engine.Scene;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
public class TransparentDrawSortTests
{
    [Fact]
    public void SortBackToFront_OrdersFartherObjectsFirst()
    {
        var camera = Vector3.Zero;
        var items = new List<(Vector3 Position, string Label)>
        {
            (new Vector3(0, 0, 2), "near"),
            (new Vector3(0, 0, 10), "far"),
            (new Vector3(0, 0, 5), "mid")
        };

        TransparentDrawSort.SortBackToFront(items, camera, static item => item.Position);

        items.Select(i => i.Label).ShouldBe(["far", "mid", "near"]);
    }

    [Fact]
    public void DistanceSquared_MatchesManualCalculation()
    {
        var camera = new Vector3(1, 2, 3);
        var target = new Vector3(4, 6, 3);
        var expected = (target - camera).LengthSquared();

        TransparentDrawSort.DistanceSquared(camera, target).ShouldBe(expected);
    }
}
