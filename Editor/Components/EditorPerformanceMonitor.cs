using System.Diagnostics;
using System.Numerics;
using ImGuiNET;

namespace Editor.Components;

public interface IEditorPerformanceMonitor
{
    float CurrentFps { get; }
    float AverageFrameTime { get; }
    void UpdateFpsTracking(TimeSpan timeSpan);
    void TrackPanelPerformance(string panelName, Action renderAction);
    void RenderPerformanceStats();
    void Reset();
}

public class EditorPerformanceMonitor : IEditorPerformanceMonitor
{
    private readonly Queue<float> _frameTimes = new();
    private readonly Dictionary<string, PanelPerformanceData> _panelPerformance = new();
    private float _fpsUpdateTimer = 0.0f;
    private float _currentFps = 0.0f;
    private const float FpsUpdateInterval = 0.1f;
    private const int MaxFrameSamples = 60;

    public float CurrentFps => _currentFps;
    public float AverageFrameTime => _frameTimes.Count > 0 ? _frameTimes.Average() : 0f;

    public void UpdateFpsTracking(TimeSpan timeSpan)
    {
        float deltaTime = (float)timeSpan.TotalSeconds;
        
        if (deltaTime <= 0) return;
        
        _frameTimes.Enqueue(deltaTime);
        
        while (_frameTimes.Count > MaxFrameSamples)
        {
            _frameTimes.Dequeue();
        }
        
        _fpsUpdateTimer += deltaTime;
        if (_fpsUpdateTimer >= FpsUpdateInterval)
        {
            CalculateFps();
            _fpsUpdateTimer = 0.0f;
        }
    }

    public void TrackPanelPerformance(string panelName, Action renderAction)
    {
        var stopwatch = Stopwatch.StartNew();
        renderAction();
        stopwatch.Stop();
        
        if (!_panelPerformance.TryGetValue(panelName, out var data))
        {
            data = new PanelPerformanceData();
            _panelPerformance[panelName] = data;
        }
        
        data.AddSample(stopwatch.ElapsedMilliseconds);
    }

    public void RenderPerformanceStats()
    {
        ImGui.Separator();
        ImGui.Text("Performance:");
        
        var fpsColor = _currentFps >= 60.0f ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) :  
                       _currentFps >= 30.0f ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) :  
                                             new Vector4(1.0f, 0.0f, 0.0f, 1.0f);    
        
        ImGui.PushStyleColor(ImGuiCol.Text, fpsColor);
        ImGui.Text($"FPS: {_currentFps:F1}");
        ImGui.PopStyleColor();
        
        float currentFrameTime = _frameTimes.Count > 0 ? _frameTimes.Last() * 1000 : 0;
        ImGui.Text($"Frame Time: {currentFrameTime:F2} ms");
        
        ImGui.Text($"Frame Samples: {_frameTimes.Count}/{MaxFrameSamples}");
        
        if (_frameTimes.Count > 1)
        {
            float minFrameTime = _frameTimes.Min() * 1000;
            float maxFrameTime = _frameTimes.Max() * 1000;
            ImGui.Text($"Min/Max Frame Time: {minFrameTime:F2}/{maxFrameTime:F2} ms");
        }
        
        if (_panelPerformance.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text("Panel Performance:");
            foreach (var kvp in _panelPerformance)
            {
                ImGui.Text($"{kvp.Key}: {kvp.Value.AverageTime:F2} ms");
            }
        }
    }

    public void Reset()
    {
        _frameTimes.Clear();
        _panelPerformance.Clear();
        _fpsUpdateTimer = 0.0f;
        _currentFps = 0.0f;
    }

    private void CalculateFps()
    {
        if (_frameTimes.Count == 0) return;
        
        float averageFrameTime = _frameTimes.Average();
        _currentFps = 1.0f / averageFrameTime;
    }

    private class PanelPerformanceData
    {
        private readonly Queue<float> _samples = new();
        private const int MaxSamples = 30;
        
        public float AverageTime => _samples.Count > 0 ? _samples.Average() : 0f;
        
        public void AddSample(float timeMs)
        {
            _samples.Enqueue(timeMs);
            while (_samples.Count > MaxSamples)
            {
                _samples.Dequeue();
            }
        }
    }
}