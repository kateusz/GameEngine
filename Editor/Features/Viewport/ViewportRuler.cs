using System.Numerics;
using ImGuiNET;

namespace Editor.Features.Viewport;

/// <summary>
/// Renders horizontal and vertical rulers in the viewport, similar to Godot's 2D editor rulers.
/// </summary>
public class ViewportRuler
{
    private const float RulerThickness = 20.0f;
    private const float MajorTickSize = 10.0f;
    private const float MinorTickSize = 5.0f;
    private const float TextOffset = 2.0f;
    
    // Lazy initialization to avoid calling ImGui before it's ready
    private static uint BackgroundColor => ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 0.95f));
    private static uint LineColor => ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
    private static uint TextColor => ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
    private static uint MajorTickColor => ImGui.GetColorU32(new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
    private static uint MinorTickColor => ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

    private float _zoom = 1.0f;
    private Vector2 _cameraPosition = Vector2.Zero;

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Renders the rulers at the top and left edges of the viewport.
    /// </summary>
    /// <param name="viewportMin">Top-left corner of the viewport in screen space</param>
    /// <param name="viewportMax">Bottom-right corner of the viewport in screen space</param>
    /// <param name="cameraPosition">Current camera position in world space</param>
    /// <param name="zoom">Current zoom level (orthographic size)</param>
    public void Render(Vector2 viewportMin, Vector2 viewportMax, Vector2 cameraPosition, float zoom = 1.0f)
    {
        if (!Enabled)
            return;

        _zoom = zoom;
        _cameraPosition = cameraPosition;
        
        var drawList = ImGui.GetWindowDrawList();
        var viewportSize = viewportMax - viewportMin;
        
        DrawHorizontalRuler(drawList, viewportMin, viewportSize);
        DrawVerticalRuler(drawList, viewportMin, viewportSize);
        DrawCornerSquare(drawList, viewportMin);
    }

    private void DrawHorizontalRuler(ImDrawListPtr drawList, Vector2 viewportMin, Vector2 viewportSize)
    {
        var rulerMin = viewportMin;
        var rulerMax = new Vector2(viewportMin.X + viewportSize.X, viewportMin.Y + RulerThickness);
        
        // Background
        drawList.AddRectFilled(rulerMin, rulerMax, BackgroundColor);
        
        // Bottom border line
        drawList.AddLine(
            new Vector2(rulerMin.X, rulerMax.Y),
            new Vector2(rulerMax.X, rulerMax.Y),
            LineColor);

        // Calculate world space range visible in viewport
        var worldWidth = viewportSize.X / _zoom;
        var worldLeft = _cameraPosition.X - worldWidth / 2.0f;
        
        // Determine tick spacing based on zoom
        var tickSpacing = ViewportScaleHelper.CalculateTickSpacing(_zoom);
        var majorTickInterval = tickSpacing * 10.0f;
        
        // Find the first major tick to draw
        var firstMajorTick = (float)Math.Floor(worldLeft / majorTickInterval) * majorTickInterval;
        
        // Draw ticks
        for (var worldX = firstMajorTick; worldX < worldLeft + worldWidth; worldX += tickSpacing)
        {
            var screenX = ViewportScaleHelper.WorldToScreenX(worldX, _cameraPosition.X, _zoom, viewportMin.X, viewportSize.X);
            
            if (screenX < viewportMin.X || screenX > viewportMin.X + viewportSize.X)
                continue;
            
            var isMajorTick = Math.Abs(worldX % majorTickInterval) < 0.001f;
            
            if (isMajorTick)
            {
                // Major tick
                drawList.AddLine(
                    new Vector2(screenX, rulerMax.Y - MajorTickSize),
                    new Vector2(screenX, rulerMax.Y),
                    MajorTickColor, 1.5f);
                
                // Label
                var label = $"{(int)worldX}";
                var textSize = ImGui.CalcTextSize(label);
                drawList.AddText(
                    new Vector2(screenX - textSize.X / 2.0f, rulerMin.Y + TextOffset),
                    TextColor,
                    label);
            }
            else
            {
                // Minor tick
                drawList.AddLine(
                    new Vector2(screenX, rulerMax.Y - MinorTickSize),
                    rulerMax with { X = screenX },
                    MinorTickColor);
            }
        }
    }

    private void DrawVerticalRuler(ImDrawListPtr drawList, Vector2 viewportMin, Vector2 viewportSize)
    {
        var rulerMin = viewportMin with { Y = viewportMin.Y + RulerThickness };
        var rulerMax = new Vector2(viewportMin.X + RulerThickness, viewportMin.Y + viewportSize.Y);
        
        // Background
        drawList.AddRectFilled(rulerMin, rulerMax, BackgroundColor);
        
        // Right border line
        drawList.AddLine(
            new Vector2(rulerMax.X, rulerMin.Y),
            new Vector2(rulerMax.X, rulerMax.Y),
            LineColor);

        // Calculate world space range visible in viewport
        var worldHeight = viewportSize.Y / _zoom;
        var worldTop = _cameraPosition.Y + worldHeight / 2.0f;
        
        // Determine tick spacing based on zoom
        var tickSpacing = ViewportScaleHelper.CalculateTickSpacing(_zoom);
        var majorTickInterval = tickSpacing * 10.0f;
        
        // Find the first major tick to draw
        var firstMajorTick = (float)Math.Ceiling((worldTop - worldHeight) / majorTickInterval) * majorTickInterval;
        
        // Draw ticks
        for (var worldY = firstMajorTick; worldY < worldTop; worldY += tickSpacing)
        {
            var screenY = ViewportScaleHelper.WorldToScreenY(worldY, _cameraPosition.Y, _zoom,
                viewportMin.Y + RulerThickness, viewportSize.Y - RulerThickness);
            
            if (screenY < viewportMin.Y + RulerThickness || screenY > viewportMin.Y + viewportSize.Y)
                continue;
            
            var isMajorTick = Math.Abs(worldY % majorTickInterval) < 0.001f;
            
            if (isMajorTick)
            {
                // Major tick
                drawList.AddLine(
                    new Vector2(rulerMax.X - MajorTickSize, screenY),
                    rulerMax with { Y = screenY },
                    MajorTickColor, 1.5f);
                
                // Label (rotated text would be nice but ImGui doesn't support it easily)
                var label = $"{(int)worldY}";
                var textSize = ImGui.CalcTextSize(label);
                drawList.AddText(
                    new Vector2(rulerMin.X + TextOffset, screenY - textSize.Y / 2.0f),
                    TextColor,
                    label);
            }
            else
            {
                // Minor tick
                drawList.AddLine(
                    new Vector2(rulerMax.X - MinorTickSize, screenY),
                    rulerMax with { Y = screenY },
                    MinorTickColor);
            }
        }
    }

    private void DrawCornerSquare(ImDrawListPtr drawList, Vector2 viewportMin)
    {
        var squareMin = viewportMin;
        var squareMax = new Vector2(viewportMin.X + RulerThickness, viewportMin.Y + RulerThickness);
        
        drawList.AddRectFilled(squareMin, squareMax, BackgroundColor);
        
        // Border lines
        drawList.AddLine(
            new Vector2(squareMax.X, squareMin.Y),
            squareMax,
            LineColor);
        drawList.AddLine(
            new Vector2(squareMin.X, squareMax.Y),
            squareMax,
            LineColor);
    }
}

