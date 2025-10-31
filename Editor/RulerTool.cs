using System.Numerics;
using Editor.Utilities;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor;

/// <summary>
/// Handles ruler/measurement tool in the editor viewport (similar to Godot's ruler mode).
/// Allows measuring distances between two points.
/// </summary>
public class RulerTool
{
    private bool _isMeasuring;
    private Vector2 _startPoint;
    private Vector2 _endPoint;
    private bool _hasStartPoint;

    // Expose measuring state so other editor systems can query it
    public bool IsMeasuring => _isMeasuring;


    /// <summary>
    /// Starts a new measurement from the given screen position.
    /// </summary>
    public void StartMeasurement(Vector2 screenPos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _startPoint = ViewportCoordinateConverter.ScreenToWorld(screenPos, viewportBounds, camera);
        _endPoint = _startPoint;
        _hasStartPoint = true;
        _isMeasuring = true;
    }

    /// <summary>
    /// Updates the end point of the measurement.
    /// </summary>
    public void UpdateMeasurement(Vector2 screenPos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_isMeasuring)
            return;
        
        _endPoint = ViewportCoordinateConverter.ScreenToWorld(screenPos, viewportBounds, camera);
    }

    /// <summary>
    /// Ends the current measurement.
    /// </summary>
    public void EndMeasurement()
    {
        _isMeasuring = false;
        _hasStartPoint = false;
    }

    /// <summary>
    /// Clears the current measurement (ESC key).
    /// </summary>
    public void ClearMeasurement()
    {
        _isMeasuring = false;
        _hasStartPoint = false;
    }

    /// <summary>
    /// Renders the measurement line and information overlay.
    /// </summary>
    public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_hasStartPoint)
            return;

        var drawList = ImGui.GetWindowDrawList();

        // Convert world points to screen space
        var startScreen = ViewportCoordinateConverter.WorldToScreen(_startPoint, viewportBounds, camera);
        var endScreen = ViewportCoordinateConverter.WorldToScreen(_endPoint, viewportBounds, camera);

        // Draw line
        var lineColor = ImGui.GetColorU32(new Vector4(1.0f, 0.8f, 0.2f, 1.0f)); // Yellow
        drawList.AddLine(startScreen, endScreen, lineColor, 2.0f);

        // Draw start and end point circles
        var startColor = ImGui.GetColorU32(new Vector4(0.2f, 0.8f, 0.2f, 1.0f)); // Green
        drawList.AddCircleFilled(startScreen, 4.0f, startColor);
        var endColor = _isMeasuring ? ImGui.GetColorU32(new Vector4(0.8f, 0.2f, 0.2f, 1.0f)) : startColor;
        drawList.AddCircleFilled(endScreen, 4.0f, endColor);

        // Calculate distance and text
        var distance = Vector2.Distance(_startPoint, _endPoint);
        var deltaX = _endPoint.X - _startPoint.X;
        var deltaY = _endPoint.Y - _startPoint.Y;

        var cameraPos = camera.Position;
        var viewportSize = viewportBounds[1] - viewportBounds[0];

        var measurementText = $"Distance: {distance:F2}\nΔX: {deltaX:F2}\nΔY: {deltaY:F2}\nStart: ({_startPoint.X:F2}, {_startPoint.Y:F2})\nEnd: ({_endPoint.X:F2}, {_endPoint.Y:F2})\n" +
                              $"CameraPos: ({cameraPos.X:F2}, {cameraPos.Y:F2})\nViewportSize: ({viewportSize.X:F0} x {viewportSize.Y:F0})";

        var textSize = ImGui.CalcTextSize(measurementText);
        var padding = new Vector2(8, 6);

        // By default place popup near the mouse cursor
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

        // Recalculate box after clamping
        textBoxMin = textPos - padding;
        textBoxMax = textPos + textSize + padding;

        // Draw text background and border
        var textBgColor = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
        drawList.AddRectFilled(textBoxMin, textBoxMax, textBgColor, 4.0f);
        drawList.AddRect(textBoxMin, textBoxMax, lineColor, 4.0f, ImDrawFlags.None, 1.0f);

        // Draw text
        var textColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddText(textPos, textColor, measurementText);
    }
}
