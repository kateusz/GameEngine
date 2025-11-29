using System.Numerics;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Handles ruler/measurement tool in the editor viewport (similar to Godot's ruler mode).
/// Allows measuring distances between two points.
/// </summary>
public class RulerTool : IViewportTool
{
    private bool _isMeasuring;
    private Vector2 _startPoint;
    private Vector2 _endPoint;
    private bool _hasStartPoint;

    public EditorMode Mode => EditorMode.Ruler;
    public bool IsActive => _isMeasuring;

    public void OnActivate()
    {
    }

    public void OnDeactivate() => ClearMeasurement();

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Start new measurement
        _startPoint = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
        _endPoint = _startPoint;
        _hasStartPoint = true;
        _isMeasuring = true;
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_isMeasuring)
            return;

        _endPoint = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // End measurement
        _isMeasuring = false;
        _hasStartPoint = false;
    }

    /// <summary>
    /// Clears the current measurement (ESC key or mode switch).
    /// </summary>
    public void ClearMeasurement()
    {
        _isMeasuring = false;
        _hasStartPoint = false;
    }

    public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_hasStartPoint)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var startScreen = ViewportCoordinateConverter.WorldToScreen(_startPoint, viewportBounds, camera);
        var endScreen = ViewportCoordinateConverter.WorldToScreen(_endPoint, viewportBounds, camera);

        DrawMeasurementLine(drawList, startScreen, endScreen);
        DrawMeasurementPoints(drawList, startScreen, endScreen);
        DrawMeasurementInfo(drawList, viewportBounds);
    }

    private void DrawMeasurementLine(ImDrawListPtr drawList, Vector2 startScreen, Vector2 endScreen)
    {
        var lineColor = ImGui.GetColorU32(new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellow
        drawList.AddLine(startScreen, endScreen, lineColor, 2.0f);
    }

    private void DrawMeasurementPoints(ImDrawListPtr drawList, Vector2 startScreen, Vector2 endScreen)
    {
        var startColor = ImGui.GetColorU32(new Vector4(0.2f, 0.8f, 0.2f, 1.0f)); // Green
        drawList.AddCircleFilled(startScreen, 4.0f, startColor);

        var endColor = _isMeasuring 
            ? ImGui.GetColorU32(new Vector4(0.8f, 0.2f, 0.2f, 1.0f)) // Red when measuring
            : startColor; // Green when finished
        drawList.AddCircleFilled(endScreen, 4.0f, endColor);
    }

    private void DrawMeasurementInfo(ImDrawListPtr drawList, Vector2[] viewportBounds)
    {
        var measurementText = BuildMeasurementText();
        var textPos = CalculateInfoBoxPosition(measurementText, viewportBounds);
        DrawInfoBox(drawList, textPos, measurementText);
    }

    private string BuildMeasurementText()
    {
        var distance = Vector2.Distance(_startPoint, _endPoint);
        var deltaX = _endPoint.X - _startPoint.X;
        var deltaY = _endPoint.Y - _startPoint.Y;

        return $"Distance: {distance:F2}\n" +
               $"dX: {deltaX:F2}\n" +
               $"dY: {deltaY:F2}\n" +
               $"Start: ({_startPoint.X:F2}, {_startPoint.Y:F2})\n" +
               $"End: ({_endPoint.X:F2}, {_endPoint.Y:F2})\n";
    }

    private static Vector2 CalculateInfoBoxPosition(string text, Vector2[] viewportBounds)
    {
        var textSize = ImGui.CalcTextSize(text);
        var padding = new Vector2(8, 6);
        var mouseGlobal = ImGui.GetMousePos();
        var textPos = mouseGlobal + new Vector2(10, 10);

        // Clamp popup inside viewport
        var viewportMin = viewportBounds[0];
        var viewportMax = viewportBounds[1];

        var textBoxMin = textPos - padding;
        var textBoxMax = textPos + textSize + padding;

        if (textBoxMax.X > viewportMax.X)
            textPos.X -= (textBoxMax.X - viewportMax.X) + 4;
        if (textBoxMax.Y > viewportMax.Y)
            textPos.Y -= (textBoxMax.Y - viewportMax.Y) + 4;
        if (textBoxMin.X < viewportMin.X)
            textPos.X += (viewportMin.X - textBoxMin.X) + 4;
        if (textBoxMin.Y < viewportMin.Y)
            textPos.Y += (viewportMin.Y - textBoxMin.Y) + 4;

        return textPos;
    }

    private static void DrawInfoBox(ImDrawListPtr drawList, Vector2 textPos, string text)
    {
        var textSize = ImGui.CalcTextSize(text);
        var padding = new Vector2(8, 6);
        var textBoxMin = textPos - padding;
        var textBoxMax = textPos + textSize + padding;

        // Draw background
        var bgColor = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
        drawList.AddRectFilled(textBoxMin, textBoxMax, bgColor, 4.0f);

        // Draw border
        var borderColor = ImGui.GetColorU32(new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellow
        drawList.AddRect(textBoxMin, textBoxMax, borderColor, 4.0f, ImDrawFlags.None, 1.0f);

        // Draw text
        var textColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddText(textPos, textColor, text);
    }
}
