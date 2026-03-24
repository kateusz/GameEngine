using System.Text;
using System.Text.Json;

namespace Engine.Renderer.Profiling;

public class ProfileExporter(IPerformanceProfiler profiler)
{
    public string ExportCsvString(int frameCount)
    {
        var data = profiler.GetData();
        var history = data.GetHistory(frameCount);
        var sb = new StringBuilder();

        // Header
        sb.Append("FrameNumber,TotalFrameTimeMs");
        foreach (var scope in data.RegisteredScopes)
            sb.Append($",{scope}_CpuMs,{scope}_GpuMs,{scope}_AllocBytes");
        foreach (var counter in data.RegisteredCounters)
            sb.Append($",{counter}");
        foreach (var gauge in data.RegisteredGauges)
            sb.Append($",{gauge}");
        sb.AppendLine();

        // Rows
        for (var i = 0; i < history.Length; i++)
        {
            var snap = history[i];
            sb.Append($"{snap.FrameNumber},{snap.TotalFrameTimeMs:F4}");

            for (var s = 0; s < data.RegisteredScopes.Count; s++)
            {
                var cpuMs = s < snap.ScopeTimingsMs.Length ? snap.ScopeTimingsMs.Span[s] : 0;
                var gpuMs = s < snap.GpuTimingsMs.Length ? snap.GpuTimingsMs.Span[s] : 0;
                var alloc = s < snap.Allocations.Length ? snap.Allocations.Span[s] : 0;
                sb.Append($",{cpuMs:F4},{gpuMs:F4},{alloc}");
            }

            for (var c = 0; c < data.RegisteredCounters.Count; c++)
            {
                var val = c < snap.Counters.Length ? snap.Counters.Span[c] : 0;
                sb.Append($",{val}");
            }

            for (var g = 0; g < data.RegisteredGauges.Count; g++)
            {
                var val = g < snap.Gauges.Length ? snap.Gauges.Span[g] : 0;
                sb.Append($",{val:F4}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string ExportJsonString(int frameCount)
    {
        var data = profiler.GetData();
        var history = data.GetHistory(frameCount);
        var frames = new List<Dictionary<string, object>>();

        for (var i = 0; i < history.Length; i++)
        {
            var snap = history[i];
            var frame = new Dictionary<string, object>
            {
                ["FrameNumber"] = snap.FrameNumber,
                ["TotalFrameTimeMs"] = snap.TotalFrameTimeMs,
            };

            for (var s = 0; s < data.RegisteredScopes.Count; s++)
            {
                var name = data.RegisteredScopes[s];
                frame[$"{name}_CpuMs"] = s < snap.ScopeTimingsMs.Length ? snap.ScopeTimingsMs.Span[s] : 0;
                frame[$"{name}_GpuMs"] = s < snap.GpuTimingsMs.Length ? snap.GpuTimingsMs.Span[s] : 0;
                frame[$"{name}_AllocBytes"] = s < snap.Allocations.Length ? snap.Allocations.Span[s] : 0;
            }

            for (var c = 0; c < data.RegisteredCounters.Count; c++)
                frame[data.RegisteredCounters[c]] = c < snap.Counters.Length ? snap.Counters.Span[c] : 0;

            for (var g = 0; g < data.RegisteredGauges.Count; g++)
                frame[data.RegisteredGauges[g]] = g < snap.Gauges.Length ? snap.Gauges.Span[g] : 0;

            frames.Add(frame);
        }

        return JsonSerializer.Serialize(frames, new JsonSerializerOptions { WriteIndented = true });
    }

    public void ExportToFile(string path, int frameCount, bool json = false)
    {
        var content = json ? ExportJsonString(frameCount) : ExportCsvString(frameCount);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }
}
