using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using ImGuiNET;

namespace Editor.Panels;

public class PerformanceMonitorPanel(
    DebugSettings debugSettings,
    int maxFrameSamples = 60,
    float fpsUpdateInterval = 0.1f)
{
    private readonly Queue<float> _frameTimes = new();
    private float _fpsUpdateTimer = 0.0f;
    private float _currentFps = 0.0f;

    public int MaxFrameSamples { get; } = maxFrameSamples;
    public float FpsUpdateInterval { get; } = fpsUpdateInterval;

    public void Update(TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;
        if (dt <= 0) return;

        _frameTimes.Enqueue(dt);

        while (_frameTimes.Count > MaxFrameSamples)
            _frameTimes.Dequeue();

        _fpsUpdateTimer += dt;
        if (_fpsUpdateTimer >= FpsUpdateInterval)
        {
            CalculateFps();
            _fpsUpdateTimer = 0.0f;
        }
    }

    private void CalculateFps()
    {
        if (_frameTimes.Count == 0) return;
        var avg = _frameTimes.Average();
        _currentFps = 1.0f / avg;
    }

    public void RenderUI()
    {
        // Only render FPS counter if the debug flag is enabled
        if (!debugSettings.ShowFPS)
            return;

        ImGui.Separator();
        ImGui.Text("Performance:");

        var fpsColor = _currentFps >= 60.0f ? new Vector4(0f, 1f, 0f, 1f) :
            _currentFps >= 30.0f ? new Vector4(1f, 1f, 0f, 1f) :
            new Vector4(1f, 0f, 0f, 1f);

        TextDrawer.DrawColoredText($"FPS: {_currentFps:F1}", fpsColor);

        var currentFrameTime = _frameTimes.Count > 0 ? _frameTimes.Last() * 1000 : 0;
        ImGui.Text($"Frame Time: {currentFrameTime:F2} ms");
        ImGui.Text($"Frame Samples: {_frameTimes.Count}/{MaxFrameSamples}");

        if (_frameTimes.Count > 1)
        {
            var minTime = _frameTimes.Min() * 1000;
            var maxTime = _frameTimes.Max() * 1000;
            ImGui.Text($"Min/Max Frame Time: {minTime:F2}/{maxTime:F2} ms");
        }
    }
}