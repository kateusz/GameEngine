using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class SoftwareOcclusionBufferTests
{
    // Identity-ish VP: NDC = (x * M11, y, z), aspect = M22/M11 = 16/9.
    private static Matrix4x4 FlatVp()
    {
        var m = Matrix4x4.Identity;
        m.M11 = 9f / 16f;
        m.M22 = 1f;
        m.M33 = 1f;
        return m;
    }

    private static SoftwareOcclusionBuffer Begun()
    {
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(FlatVp()).ShouldBeTrue();
        return grid;
    }

    // Huge triangle covering the whole NDC square [-1,1]^2 at constant depth.
    private static void RasterizeFullScreen(SoftwareOcclusionBuffer grid, float depth)
    {
        var sx = 16f / 9f;
        grid.RasterizeTriangle(
            new Vector3(-2f * sx, -2f, depth),
            new Vector3(2f * sx, -2f, depth),
            new Vector3(0f, 3f, depth));
    }

    [Fact]
    public void FullCoverTriangle_HidesBoxBehindIt()
    {
        var grid = Begun();
        RasterizeFullScreen(grid, 0.3f);

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeTrue();
    }

    [Fact]
    public void BoxNearerThanTriangle_IsNotHidden()
    {
        var grid = Begun();
        RasterizeFullScreen(grid, 0.3f);

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.1f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
    }

    [Fact]
    public void BoxOutsideTriangleFootprint_IsNotHidden()
    {
        var grid = Begun();
        // Right-half triangle: covers x > 0 roughly, leaves left side uncovered.
        var sx = 16f / 9f;
        grid.RasterizeTriangle(
            new Vector3(0f, -2f, 0.3f),
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(sx, 3f, 0.3f));

        var world = Matrix4x4.CreateTranslation(-0.5f * sx, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
    }

    [Fact]
    public void TriangleMissingCellCorner_DoesNotHideOverlappingBox()
    {
        var grid = Begun();
        // Tiny triangle around origin — too small to fully cover any 90-row cell.
        grid.RasterizeTriangle(
            new Vector3(-0.001f, -0.001f, 0.3f),
            new Vector3(0.001f, -0.001f, 0.3f),
            new Vector3(0f, 0.001f, 0.3f));

        var world = Matrix4x4.Identity;
        var local = new Aabb(new Vector3(-0.0005f, -0.0005f, 0.5f), new Vector3(0.0005f, 0.0005f, 0.51f));
        grid.IsOccluded(world, local).ShouldBeFalse();
    }

    [Fact]
    public void TriangleWithNegativeDepthVertex_WritesNothing()
    {
        var grid = Begun();
        var sx = 16f / 9f;
        grid.RasterizeTriangle(
            new Vector3(-2f * sx, -2f, -0.1f),
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(0f, 3f, 0.3f));

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
    }

    [Fact]
    public void NonFiniteVertex_WritesNothing()
    {
        var grid = Begun();
        var sx = 16f / 9f;
        grid.RasterizeTriangle(
            new Vector3(float.NaN, -2f, 0.3f),
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(0f, 3f, 0.3f));

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
    }

    [Fact]
    public void TryBegin_ZeroM11_FailsAndIsOccludedReturnsFalse()
    {
        var grid = new SoftwareOcclusionBuffer();
        var bad = Matrix4x4.Identity;
        bad.M11 = 0f;
        bad.M22 = 1f;
        grid.TryBegin(bad).ShouldBeFalse();
        grid.IsActive.ShouldBeFalse();
        grid.IsOccluded(Matrix4x4.Identity, Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void PipelineLikeWall_HidesPropBehind()
    {
        var eye = new Vector3(0f, 0f, 8f);
        var view = Matrix4x4.CreateLookAt(eye, Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 100f);
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(view * projection).ShouldBeTrue();

        // One triangle that covers the NDC square — a quad diagonal leaves center cells uncovered.
        grid.RasterizeTriangle(new(-40f, -40f, 0f), new(40f, -40f, 0f), new(0f, 40f, 0f));

        var propWorld = Matrix4x4.CreateTranslation(0f, 0f, -2f);
        var propLocal = new Aabb(new Vector3(-0.1f, -0.1f, 0f), new Vector3(0.1f, 0.1f, 0f));
        grid.IsOccluded(propWorld, propLocal).ShouldBeTrue(
            $"submitted={grid.SubmittedTriangles} nearestOk={grid.TryNearestDepth(propWorld, propLocal, out var n)} n={n}");
    }
}
