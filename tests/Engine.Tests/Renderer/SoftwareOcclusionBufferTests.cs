using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class SoftwareOcclusionBufferTests
{
    private const float Aspect16x9 = 16f / 9f;

    // Identity-ish VP: NDC = (x * M11, y, z). Aspect is passed separately (not from M22/M11).
    private static Matrix4x4 FlatVp()
    {
        var m = Matrix4x4.Identity;
        m.M11 = 9f / 16f;
        m.M22 = 1f;
        m.M33 = 1f;
        return m;
    }

    private static SoftwareOcclusionBuffer Begun(float aspect = Aspect16x9)
    {
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(FlatVp(), aspect).ShouldBeTrue();
        return grid;
    }

    // Huge triangle covering the whole NDC square [-1,1]^2 at constant depth.
    private static void RasterizeFullScreen(SoftwareOcclusionBuffer grid, float depth)
    {
        var sx = Aspect16x9;
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
        var sx = Aspect16x9;
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
        var sx = Aspect16x9;
        grid.RasterizeTriangle(
            new Vector3(-2f * sx, -2f, -0.1f),
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(0f, 3f, 0.3f));

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
        grid.SubmittedTriangles.ShouldBe(0);
    }

    [Fact]
    public void NonFiniteVertex_WritesNothing()
    {
        var grid = Begun();
        var sx = Aspect16x9;
        grid.RasterizeTriangle(
            new Vector3(float.NaN, -2f, 0.3f),
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(0f, 3f, 0.3f));

        var world = Matrix4x4.CreateTranslation(0f, 0f, 0.55f);
        var local = new Aabb(new Vector3(-0.05f), new Vector3(0.05f));
        grid.IsOccluded(world, local).ShouldBeFalse();
        grid.SubmittedTriangles.ShouldBe(0);
    }

    [Fact]
    public void TryBegin_NonPositiveAspect_FailsAndIsOccludedReturnsFalse()
    {
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(Matrix4x4.Identity, 0f).ShouldBeFalse();
        grid.TryBegin(Matrix4x4.Identity, -1f).ShouldBeFalse();
        grid.IsActive.ShouldBeFalse();
        grid.IsOccluded(Matrix4x4.Identity, Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void TryBegin_RotatedViewProjection_StillActiveWithExplicitAspect()
    {
        // ViewProjection M11/M22 are unreliable after yaw; aspect must come from projection alone.
        var eye = new Vector3(0f, 0f, 8f);
        var view = Matrix4x4.CreateLookAt(eye, Vector3.Zero, Vector3.UnitY)
                   * Matrix4x4.CreateRotationY(MathF.PI);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, Aspect16x9, 0.1f, 100f);
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(view * projection, Aspect16x9).ShouldBeTrue();
        grid.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void RejectedProjections_DoNotConsumeTriangleBudget()
    {
        var grid = Begun();
        for (var i = 0; i < SoftwareOcclusionBuffer.TriangleBudget + 10; i++)
        {
            grid.RasterizeTriangle(
                new Vector3(-1f, -1f, -0.1f),
                new Vector3(1f, -1f, 0.3f),
                new Vector3(0f, 1f, 0.3f));
        }

        grid.SubmittedTriangles.ShouldBe(0);
        // Budget still available for a valid triangle.
        RasterizeFullScreen(grid, 0.3f);
        grid.SubmittedTriangles.ShouldBe(1);
        grid.IsOccluded(
            Matrix4x4.CreateTranslation(0f, 0f, 0.55f),
            new Aabb(new Vector3(-0.05f), new Vector3(0.05f))).ShouldBeTrue();
    }

    [Fact]
    public void PipelineLikeWall_HidesPropBehind()
    {
        var eye = new Vector3(0f, 0f, 8f);
        var view = Matrix4x4.CreateLookAt(eye, Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, Aspect16x9, 0.1f, 100f);
        var grid = new SoftwareOcclusionBuffer();
        grid.TryBegin(view * projection, Aspect16x9).ShouldBeTrue();

        grid.RasterizeTriangle(new(-40f, -40f, 0f), new(40f, -40f, 0f), new(0f, 40f, 0f));

        var propWorld = Matrix4x4.CreateTranslation(0f, 0f, -2f);
        var propLocal = new Aabb(new Vector3(-0.1f, -0.1f, 0f), new Vector3(0.1f, 0.1f, 0f));
        grid.IsOccluded(propWorld, propLocal).ShouldBeTrue(
            $"submitted={grid.SubmittedTriangles} projected={grid.TryProjectMetrics(propWorld, propLocal, out var n, out _)} n={n}");
    }

    [Fact]
    public void BackFacingTriangle_WithFrontFacesOnly_WritesNothing()
    {
        var grid = Begun();
        var sx = Aspect16x9;
        // CW order in NDC → back face when frontFacesOnly.
        grid.RasterizeLocalTriangle(
            Matrix4x4.Identity,
            new Vector3(2f * sx, -2f, 0.3f),
            new Vector3(-2f * sx, -2f, 0.3f),
            new Vector3(0f, 3f, 0.3f),
            frontFacesOnly: true);

        grid.SubmittedTriangles.ShouldBe(0);
        grid.IsOccluded(
            Matrix4x4.CreateTranslation(0f, 0f, 0.55f),
            new Aabb(new Vector3(-0.05f), new Vector3(0.05f))).ShouldBeFalse();
    }

}