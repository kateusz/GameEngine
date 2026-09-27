using System.Numerics;
using Engine.Renderer;
using Engine.Scene;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
[Collection("PointShadowCache")]
public class PointShadowCacheTests
{
    public PointShadowCacheTests() => PointShadowCache.Clear();

    [Fact]
    public void NeedsRedraw_MovedCasterInsideRange_IsTrue()
    {
        var casters = CasterList(1, Matrix4x4.Identity);
        PointShadowCache.Replace(casters, LampList(10, Vector3.Zero, 10f));
        var moved = CasterList(1, Matrix4x4.CreateTranslation(1f, 0f, 0f));
        PointShadowCache.NeedsRedraw(10, Vector3.Zero, 10f, moved).ShouldBeTrue();
    }

    [Fact]
    public void NeedsRedraw_CasterLeavesSphere_IsTrue()
    {
        var inside = CasterList(1, Matrix4x4.Identity);
        PointShadowCache.Replace(inside, LampList(10, Vector3.Zero, 10f));
        var outside = CasterList(1, Matrix4x4.CreateTranslation(30f, 0f, 0f));
        PointShadowCache.NeedsRedraw(10, Vector3.Zero, 10f, outside).ShouldBeTrue();
    }

    [Fact]
    public void NeedsRedraw_LampMoved_IsTrue()
    {
        var casters = CasterList(1, Matrix4x4.Identity);
        PointShadowCache.Replace(casters, LampList(10, Vector3.Zero, 10f));
        PointShadowCache.NeedsRedraw(10, new Vector3(1f, 0f, 0f), 10f, casters).ShouldBeTrue();
    }

    [Fact]
    public void NeedsRedraw_UnboundedMover_IsTrue()
    {
        var casters = new List<(int Id, PointShadowCache.CasterPose Pose)>
        {
            (1, new PointShadowCache.CasterPose(Matrix4x4.Identity, default, false))
        };
        PointShadowCache.Replace(casters, LampList(10, Vector3.Zero, 10f));
        var moved = new List<(int Id, PointShadowCache.CasterPose Pose)>
        {
            (1, new PointShadowCache.CasterPose(Matrix4x4.CreateTranslation(1f, 0f, 0f), default, false))
        };
        PointShadowCache.NeedsRedraw(10, Vector3.Zero, 10f, moved).ShouldBeTrue();
    }

    [Fact]
    public void NeedsRedraw_MovedCasterOutsideBothSpheres_IsFalse()
    {
        var far = CasterList(1, Matrix4x4.CreateTranslation(30f, 0f, 0f));
        PointShadowCache.Replace(far, LampList(10, Vector3.Zero, 10f));
        var stillFar = CasterList(1, Matrix4x4.CreateTranslation(31f, 0f, 0f));
        PointShadowCache.NeedsRedraw(10, Vector3.Zero, 10f, stillFar).ShouldBeFalse();
    }

    private static List<(int Id, PointShadowCache.CasterPose Pose)> CasterList(int id, Matrix4x4 world) =>
        [(id, new PointShadowCache.CasterPose(world, Aabb.UnitCube, true))];

    private static List<(int Id, PointShadowCache.LampPose Pose)> LampList(int id, Vector3 position, float range) =>
        [(id, new PointShadowCache.LampPose(position, range))];
}
