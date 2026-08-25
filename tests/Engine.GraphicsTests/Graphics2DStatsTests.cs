using System.Numerics;
using Engine.GraphicsTests;
using Engine.Scene;
using Engine.Scene.Cameras;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class Graphics2DStatsTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void DrawQuads_EndScene_RecordsBatchStats()
    {
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -10f, 10f);
        camera.SetViewportSize(800, 600);

        fixture.Graphics2D.ResetStats();
        fixture.Graphics2D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));

        for (var i = 0; i < 100; i++)
        {
            fixture.Graphics2D.DrawQuad(
                new Vector3(i * 0.1f, 0, 0),
                new Vector2(0.5f, 0.5f),
                new Vector4(1, 0, 0, 1));
        }

        fixture.Graphics2D.EndScene();

        var stats = fixture.Graphics2D.GetStats();
        Assert.Equal(100u, stats.QuadCount);
        Assert.Equal(1u, stats.DrawCalls);
        Assert.True(stats.UploadBytes > 0);
        Assert.True(stats.BatchFillMs >= 0);
        Assert.True(stats.FlushMs >= 0);
    }

    [GraphicsFact]
    public void DrawLines_EndScene_RecordsLineStats()
    {
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -10f, 10f);
        camera.SetViewportSize(800, 600);

        fixture.Graphics2D.ResetStats();
        fixture.Graphics2D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        fixture.Graphics2D.DrawLine(Vector3.Zero, Vector3.UnitX, Vector4.One, entityId: 1);
        fixture.Graphics2D.EndScene();

        var stats = fixture.Graphics2D.GetStats();
        Assert.Equal(1u, stats.LineDrawCalls);
        Assert.Equal(2u, stats.LineVertexCount);
        Assert.True(stats.UploadBytes > 0);
    }
}
