using System.Numerics;
using Engine.Core;
using ImGuiNET;

namespace Engine.Renderer.Profiling;

public class PerformanceOverlayPanel(
    IPerformanceProfiler profiler,
    DebugSettings debugSettings)
{
    private (string Name, double Ms)[] _scopeBuffer = new (string, double)[64];

    public void Draw()
    {
        if (!debugSettings.ShowPerformanceOverlay || !profiler.Enabled) return;

        var data = profiler.GetData();
        var latest = data.Latest;

        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(0.5f);

        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##PerfOverlay", flags))
        {
            var fps = latest.TotalFrameTimeMs > 0 ? 1000.0 / latest.TotalFrameTimeMs : 0;
            var fpsColor = fps >= 60 ? new Vector4(0, 1, 0, 1)
                : fps >= 30 ? new Vector4(1, 1, 0, 1)
                : new Vector4(1, 0, 0, 1);

            ImGui.TextColored(fpsColor, $"FPS: {fps:F0}");
            ImGui.Text($"Frame: {latest.TotalFrameTimeMs:F2}ms");
            ImGui.Text($"Draw Calls: {data.GetCounterValue("DrawCalls") + data.GetCounterValue("DrawCalls3D")}");
            ImGui.Text($"Batches: {data.GetCounterValue("BatchFlushes")}");

            var scopes = data.RegisteredScopes;
            if (scopes.Count > 0)
            {
                ImGui.Separator();
                if (_scopeBuffer.Length < scopes.Count)
                    _scopeBuffer = new (string, double)[scopes.Count];

                for (var i = 0; i < scopes.Count; i++)
                    _scopeBuffer[i] = (scopes[i], data.GetScopeTimingMs(scopes[i]));

                // Partial sort: find top 3 by swapping into front positions
                var count = System.Math.Min(3, scopes.Count);
                for (var i = 0; i < count; i++)
                {
                    var maxIdx = i;
                    for (var j = i + 1; j < scopes.Count; j++)
                        if (_scopeBuffer[j].Ms > _scopeBuffer[maxIdx].Ms)
                            maxIdx = j;
                    if (maxIdx != i)
                        (_scopeBuffer[i], _scopeBuffer[maxIdx]) = (_scopeBuffer[maxIdx], _scopeBuffer[i]);
                }

                for (var i = 0; i < count; i++)
                    if (_scopeBuffer[i].Ms > 0)
                        ImGui.Text($"{_scopeBuffer[i].Name}: {_scopeBuffer[i].Ms:F2}ms");
            }

            long totalAlloc = 0;
            for (var i = 0; i < data.RegisteredScopes.Count; i++)
                totalAlloc += data.GetAllocation(data.RegisteredScopes[i]);
            if (totalAlloc > 0)
            {
                ImGui.Separator();
                ImGui.Text($"Allocs: {totalAlloc:N0} bytes");
            }
        }
        ImGui.End();
    }
}
