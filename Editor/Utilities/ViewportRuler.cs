using System.Numerics;
using ImGuiNET;

namespace Editor.Utilities;

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
    private uint BackgroundColor => ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 0.95f));
    private uint LineColor => ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
    private uint TextColor => ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
    private uint MajorTickColor => ImGui.GetColorU32(new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
    private uint MinorTickColor => ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
    
    private bool _enabled = true;
    private float _zoom = 1.0f;
    private Vector2 _cameraPosition = Vector2.Zero;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    /// Renders the rulers at the top and left edges of the viewport.
    /// </summary>
    /// <param name="viewportMin">Top-left corner of the viewport in screen space</param>
    /// <param name="viewportMax">Bottom-right corner of the viewport in screen space</param>
    /// <param name="cameraPosition">Current camera position in world space</param>
    /// <param name="zoom">Current zoom level (orthographic size)</param>
    public void Render(Vector2 viewportMin, Vector2 viewportMax, Vector2 cameraPosition, float zoom = 1.0f)
    {
        if (!_enabled)
            return;

        _zoom = zoom;
        _cameraPosition = cameraPosition;
        
        var drawList = ImGui.GetWindowDrawList();
        var viewportSize = viewportMax - viewportMin;

        // Draw horizontal ruler (top)
        DrawHorizontalRuler(drawList, viewportMin, viewportSize);
        
        // Draw vertical ruler (left)
        DrawVerticalRuler(drawList, viewportMin, viewportSize);
        
        // Draw corner square
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
        float worldWidth = viewportSize.X / _zoom;
        float worldLeft = _cameraPosition.X - worldWidth / 2.0f;
        
        // Determine tick spacing based on zoom
        float tickSpacing = CalculateTickSpacing(_zoom);
        float majorTickInterval = tickSpacing * 10.0f;
        
        // Find the first major tick to draw
        float firstMajorTick = (float)Math.Floor(worldLeft / majorTickInterval) * majorTickInterval;
        
        // Draw ticks
        for (float worldX = firstMajorTick; worldX < worldLeft + worldWidth; worldX += tickSpacing)
        {
            float screenX = WorldToScreenX(worldX, viewportMin.X, viewportSize.X);
            
            if (screenX < viewportMin.X || screenX > viewportMin.X + viewportSize.X)
                continue;
            
            bool isMajorTick = Math.Abs(worldX % majorTickInterval) < 0.001f;
            
            if (isMajorTick)
            {
                // Major tick
                drawList.AddLine(
                    new Vector2(screenX, rulerMax.Y - MajorTickSize),
                    new Vector2(screenX, rulerMax.Y),
                    MajorTickColor, 1.5f);
                
                // Label
                string label = $"{(int)worldX}";
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
                    new Vector2(screenX, rulerMax.Y),
                    MinorTickColor);
            }
        }
    }

    private void DrawVerticalRuler(ImDrawListPtr drawList, Vector2 viewportMin, Vector2 viewportSize)
    {
        var rulerMin = new Vector2(viewportMin.X, viewportMin.Y + RulerThickness);
        var rulerMax = new Vector2(viewportMin.X + RulerThickness, viewportMin.Y + viewportSize.Y);
        
        // Background
        drawList.AddRectFilled(rulerMin, rulerMax, BackgroundColor);
        
        // Right border line
        drawList.AddLine(
            new Vector2(rulerMax.X, rulerMin.Y),
            new Vector2(rulerMax.X, rulerMax.Y),
            LineColor);

        // Calculate world space range visible in viewport
        float worldHeight = viewportSize.Y / _zoom;
        float worldTop = _cameraPosition.Y + worldHeight / 2.0f;
        
        // Determine tick spacing based on zoom
        float tickSpacing = CalculateTickSpacing(_zoom);
        float majorTickInterval = tickSpacing * 10.0f;
        
        // Find the first major tick to draw
        float firstMajorTick = (float)Math.Ceiling((worldTop - worldHeight) / majorTickInterval) * majorTickInterval;
        
        // Draw ticks
        for (float worldY = firstMajorTick; worldY < worldTop; worldY += tickSpacing)
        {
            float screenY = WorldToScreenY(worldY, viewportMin.Y + RulerThickness, viewportSize.Y - RulerThickness);
            
            if (screenY < viewportMin.Y + RulerThickness || screenY > viewportMin.Y + viewportSize.Y)
                continue;
            
            bool isMajorTick = Math.Abs(worldY % majorTickInterval) < 0.001f;
            
            if (isMajorTick)
            {
                // Major tick
                drawList.AddLine(
                    new Vector2(rulerMax.X - MajorTickSize, screenY),
                    new Vector2(rulerMax.X, screenY),
                    MajorTickColor, 1.5f);
                
                // Label (rotated text would be nice but ImGui doesn't support it easily)
                string label = $"{(int)worldY}";
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
                    new Vector2(rulerMax.X, screenY),
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

    private float WorldToScreenX(float worldX, float viewportMinX, float viewportWidth)
    {
        float worldWidth = viewportWidth / _zoom;
        float worldLeft = _cameraPosition.X - worldWidth / 2.0f;
        float normalizedX = (worldX - worldLeft) / worldWidth;
        return viewportMinX + normalizedX * viewportWidth;
    }

    private float WorldToScreenY(float worldY, float viewportMinY, float viewportHeight)
    {
        float worldHeight = viewportHeight / _zoom;
        float worldTop = _cameraPosition.Y + worldHeight / 2.0f;
        float normalizedY = (worldTop - worldY) / worldHeight;
        return viewportMinY + normalizedY * viewportHeight;
    }

    private float CalculateTickSpacing(float zoom)
    {
        // Calculate optimal tick spacing based on zoom level
        // We want ticks to be roughly 50-100 pixels apart
        float pixelsPerUnit = zoom;
        float targetPixelSpacing = 50.0f;
        
        float rawSpacing = targetPixelSpacing / pixelsPerUnit;
        
        // Round to nice numbers (1, 2, 5, 10, 20, 50, 100, etc.)
        float magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(rawSpacing)));
        float normalizedSpacing = rawSpacing / magnitude;
        
        float niceSpacing;
        if (normalizedSpacing < 2.0f)
            niceSpacing = 1.0f;
        else if (normalizedSpacing < 5.0f)
            niceSpacing = 2.0f;
        else
            niceSpacing = 5.0f;
        
        return niceSpacing * magnitude;
    }
}

