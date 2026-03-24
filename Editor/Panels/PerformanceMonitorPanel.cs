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
    private float _runningSum;
    private float _minFrameTime = float.MaxValue;
    private float _maxFrameTime;
    private float _lastFrameTime;

    public int MaxFrameSamples { get; } = maxFrameSamples;
    public float FpsUpdateInterval { get; } = fpsUpdateInterval;

    public void Update(TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;
        if (dt <= 0) return;

        _lastFrameTime = dt;
        _runningSum += dt;
        _frameTimes.Enqueue(dt);
        if (_frameTimes.Count > MaxFrameSamples)
        {
            var removed = _frameTimes.Dequeue();
            _runningSum -= removed;
        }

        _minFrameTime = float.MaxValue;
        _maxFrameTime = 0;
        foreach (var ft in _frameTimes)
        {
            if (ft < _minFrameTime) _minFrameTime = ft;
            if (ft > _maxFrameTime) _maxFrameTime = ft;
        }

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
        var avg = _frameTimes.Count > 0 ? _runningSum / _frameTimes.Count : 0f;
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

        var currentFrameTime = _frameTimes.Count > 0 ? _lastFrameTime * 1000 : 0;
        ImGui.Text($"Frame Time: {currentFrameTime:F2} ms");
        ImGui.Text($"Frame Samples: {_frameTimes.Count}/{MaxFrameSamples}");

        if (_frameTimes.Count > 1)
        {
            var minTime = _minFrameTime * 1000;
            var maxTime = _maxFrameTime * 1000;
            ImGui.Text($"Min/Max Frame Time: {minTime:F2}/{maxTime:F2} ms");
        }
    }
}