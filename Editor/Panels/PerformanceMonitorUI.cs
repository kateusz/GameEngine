using System.Numerics;
using Engine.Core;
using ImGuiNET;

namespace Editor.Panels;

public class PerformanceMonitorUI
{
    private readonly Queue<float> _frameTimes = new();
    private float _fpsUpdateTimer = 0.0f;
    private float _currentFps = 0.0f;
    private readonly DebugSettings _debugSettings;

    public int MaxFrameSamples { get; }
    public float FpsUpdateInterval { get; }

    public PerformanceMonitorUI(DebugSettings debugSettings, int maxFrameSamples = 60, float fpsUpdateInterval = 0.1f)
    {
        _debugSettings = debugSettings;
        MaxFrameSamples = maxFrameSamples;
        FpsUpdateInterval = fpsUpdateInterval;
    }

    public void Update(TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;
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
        float avg = _frameTimes.Average();
        _currentFps = 1.0f / avg;
    }

    public void RenderUI()
    {
        // Only render FPS counter if the debug flag is enabled
        if (!_debugSettings.ShowFPS)
            return;

        ImGui.Separator();
        ImGui.Text("Performance:");

        var fpsColor = _currentFps >= 60.0f ? new Vector4(0f, 1f, 0f, 1f) :
            _currentFps >= 30.0f ? new Vector4(1f, 1f, 0f, 1f) :
            new Vector4(1f, 0f, 0f, 1f);

        ImGui.PushStyleColor(ImGuiCol.Text, fpsColor);
        ImGui.Text($"FPS: {_currentFps:F1}");
        ImGui.PopStyleColor();

        float currentFrameTime = _frameTimes.Count > 0 ? _frameTimes.Last() * 1000 : 0;
        ImGui.Text($"Frame Time: {currentFrameTime:F2} ms");
        ImGui.Text($"Frame Samples: {_frameTimes.Count}/{MaxFrameSamples}");

        if (_frameTimes.Count > 1)
        {
            float minTime = _frameTimes.Min() * 1000;
            float maxTime = _frameTimes.Max() * 1000;
            ImGui.Text($"Min/Max Frame Time: {minTime:F2}/{maxTime:F2} ms");
        }
    }
}