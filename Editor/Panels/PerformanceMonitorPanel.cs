using System.Numerics;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Profiling;
using ImGuiNET;

namespace Editor.Panels;

public class PerformanceMonitorPanel(
    DebugSettings debugSettings,
    IPerformanceProfiler profiler,
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
    private float[] _plotBuffer = new float[300];

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

        if (profiler.Enabled)
        {
            var data = profiler.GetData();
            var history = data.GetHistory(System.Math.Min(MaxFrameSamples, 300));

            if (history.Length > 0)
            {
                if (_plotBuffer.Length < history.Length)
                    _plotBuffer = new float[history.Length];

                for (var i = 0; i < history.Length; i++)
                    _plotBuffer[i] = (float)history[i].TotalFrameTimeMs;

                ImGui.PlotLines("Frame Time (ms)", ref _plotBuffer[0], history.Length,
                    0, null, 0, 33.3f, new Vector2(0, 80));
            }
        }
    }
}