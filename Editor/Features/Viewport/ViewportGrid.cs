using System.Numerics;
using Engine.Renderer;
using ImGuiNET;

namespace Editor.Features.Viewport;

/// <summary>
/// Renders a thin-line 2D grid overlay in the editor viewport.
/// </summary>
public class ViewportGrid(IViewportScaleHelper viewportScaleHelper)
{
    /// <summary>
    /// Renders the grid. Must be called within an active ImGui window.
    /// </summary>
    /// <param name="viewportMin">Top-left corner of the viewport in screen space.</param>
    /// <param name="viewportMax">Bottom-right corner of the viewport in screen space.</param>
    /// <param name="cameraPosition">Current camera focal point in world space (X, Y).</param>
    /// <param name="zoom">Pixels per world unit (viewport height / visible world height).</param>
    public void Render(Vector2 viewportMin, Vector2 viewportMax, Vector2 cameraPosition, float zoom)
    {
        if (zoom <= 0)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var viewportSize = viewportMax - viewportMin;
        var tickSpacing = viewportScaleHelper.CalculateTickSpacing(zoom);
        var minorColor = ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, RenderingConstants.GridMinorLineOpacity));
        var majorColor = ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, RenderingConstants.GridMajorLineOpacity));

        var ctx = new GridDrawContext(drawList, viewportMin, viewportMax, viewportSize, cameraPosition, zoom, tickSpacing, minorColor, majorColor);
        DrawVerticalLines(ctx);
        DrawHorizontalLines(ctx);
    }

    private void DrawVerticalLines(GridDrawContext ctx)
    {
        var worldWidth = ctx.ViewportSize.X / ctx.Zoom;
        var worldLeft = ctx.CameraPosition.X - worldWidth / 2.0f;
        var firstTick = (float)Math.Floor(worldLeft / ctx.TickSpacing) * ctx.TickSpacing;

        for (var worldX = firstTick; worldX <= worldLeft + worldWidth; worldX += ctx.TickSpacing)
        {
            var screenX = viewportScaleHelper.WorldToScreenX(worldX, ctx.CameraPosition.X, ctx.Zoom,
                ctx.ViewportMin.X, ctx.ViewportSize.X);

            if (screenX < ctx.ViewportMin.X || screenX > ctx.ViewportMax.X)
                continue;

            var isMajor = (int)Math.Round(worldX / ctx.TickSpacing) % RenderingConstants.GridMajorStep == 0;
            var color = isMajor ? ctx.MajorColor : ctx.MinorColor;

            ctx.DrawList.AddLine(
                ctx.ViewportMin with { X = screenX },
                ctx.ViewportMax with { X = screenX },
                color);
        }
    }

    private void DrawHorizontalLines(GridDrawContext ctx)
    {
        var worldHeight = ctx.ViewportSize.Y / ctx.Zoom;
        var worldTop = ctx.CameraPosition.Y + worldHeight / 2.0f;
        var firstTick = (float)Math.Floor((worldTop - worldHeight) / ctx.TickSpacing) * ctx.TickSpacing;

        for (var worldY = firstTick; worldY <= worldTop; worldY += ctx.TickSpacing)
        {
            var screenY = viewportScaleHelper.WorldToScreenY(worldY, ctx.CameraPosition.Y, ctx.Zoom,
                ctx.ViewportMin.Y, ctx.ViewportSize.Y);

            if (screenY < ctx.ViewportMin.Y || screenY > ctx.ViewportMax.Y)
                continue;

            var isMajor = (int)Math.Round(worldY / ctx.TickSpacing) % RenderingConstants.GridMajorStep == 0;
            var color = isMajor ? ctx.MajorColor : ctx.MinorColor;

            ctx.DrawList.AddLine(
                ctx.ViewportMin with { Y = screenY },
                ctx.ViewportMax with { Y = screenY },
                color);
        }
    }

    private readonly record struct GridDrawContext(
        ImDrawListPtr DrawList,
        Vector2 ViewportMin,
        Vector2 ViewportMax,
        Vector2 ViewportSize,
        Vector2 CameraPosition,
        float Zoom,
        float TickSpacing,
        uint MinorColor,
        uint MajorColor);
}
