using System.Numerics;
using ImGuiNET;

namespace Editor.Features.Viewport;

/// <summary>
/// Renders a thin-line 2D grid overlay in the editor viewport.
/// </summary>
public class ViewportGrid
{
    private const float MinorLineOpacity = 0.30f;
    private const float MajorLineOpacity = 0.55f;

    private uint MinorLineColor => ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, MinorLineOpacity));
    private uint MajorLineColor => ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, MajorLineOpacity));

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Renders the grid. Must be called within an active ImGui window.
    /// </summary>
    /// <param name="viewportMin">Top-left corner of the viewport in screen space.</param>
    /// <param name="viewportMax">Bottom-right corner of the viewport in screen space.</param>
    /// <param name="cameraPosition">Current camera focal point in world space (X, Y).</param>
    /// <param name="zoom">Pixels per world unit (viewport height / visible world height).</param>
    public void Render(Vector2 viewportMin, Vector2 viewportMax, Vector2 cameraPosition, float zoom)
    {
        if (!Enabled)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var viewportSize = viewportMax - viewportMin;

        var tickSpacing = ViewportScaleHelper.CalculateTickSpacing(zoom);
        var majorTickInterval = tickSpacing * 10.0f;

        DrawVerticalLines(drawList, viewportMin, viewportMax, viewportSize, cameraPosition, zoom,
            tickSpacing, majorTickInterval);
        DrawHorizontalLines(drawList, viewportMin, viewportMax, viewportSize, cameraPosition, zoom,
            tickSpacing, majorTickInterval);
    }

    private void DrawVerticalLines(ImDrawListPtr drawList, Vector2 viewportMin, Vector2 viewportMax,
        Vector2 viewportSize, Vector2 cameraPosition, float zoom, float tickSpacing, float majorTickInterval)
    {
        var worldWidth = viewportSize.X / zoom;
        var worldLeft = cameraPosition.X - worldWidth / 2.0f;
        var firstTick = (float)Math.Floor(worldLeft / tickSpacing) * tickSpacing;

        for (var worldX = firstTick; worldX <= worldLeft + worldWidth; worldX += tickSpacing)
        {
            var screenX = ViewportScaleHelper.WorldToScreenX(worldX, cameraPosition.X, zoom,
                viewportMin.X, viewportSize.X);

            if (screenX < viewportMin.X || screenX > viewportMax.X)
                continue;

            var isMajor = Math.Abs(worldX % majorTickInterval) < tickSpacing * 0.01f;
            var color = isMajor ? MajorLineColor : MinorLineColor;

            drawList.AddLine(
                new Vector2(screenX, viewportMin.Y),
                new Vector2(screenX, viewportMax.Y),
                color);
        }
    }

    private void DrawHorizontalLines(ImDrawListPtr drawList, Vector2 viewportMin, Vector2 viewportMax,
        Vector2 viewportSize, Vector2 cameraPosition, float zoom, float tickSpacing, float majorTickInterval)
    {
        var worldHeight = viewportSize.Y / zoom;
        var worldTop = cameraPosition.Y + worldHeight / 2.0f;
        var firstTick = (float)Math.Floor((worldTop - worldHeight) / tickSpacing) * tickSpacing;

        for (var worldY = firstTick; worldY <= worldTop; worldY += tickSpacing)
        {
            var screenY = ViewportScaleHelper.WorldToScreenY(worldY, cameraPosition.Y, zoom,
                viewportMin.Y, viewportSize.Y);

            if (screenY < viewportMin.Y || screenY > viewportMax.Y)
                continue;

            var isMajor = Math.Abs(worldY % majorTickInterval) < tickSpacing * 0.01f;
            var color = isMajor ? MajorLineColor : MinorLineColor;

            drawList.AddLine(
                new Vector2(viewportMin.X, screenY),
                new Vector2(viewportMax.X, screenY),
                color);
        }
    }
}
