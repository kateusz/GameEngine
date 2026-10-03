using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Serializer;
using ImGuiNET;

namespace Benchmark;

public class BenchmarkLayer(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    ITextureFactory textureFactory,
    IModelFactory modelFactory,
    ISceneSerializer scenes)
    : ILayer
{
    private readonly Benchmark2DRunner _twoD = new(graphics2D, sceneFactory, textureFactory);
    private readonly Benchmark3DRunner _threeD = new(sceneFactory, graphics2D, graphics3D, textureFactory, modelFactory, scenes);
    private readonly BenchmarkGridRunner _lighting = new("Lighting", sceneFactory, graphics2D, graphics3D, textureFactory, modelFactory);
    private readonly BenchmarkGridRunner _shadows = new("Shadows", sceneFactory, graphics2D, graphics3D, textureFactory, modelFactory);
    private readonly ActiveRun _active = new();
    private readonly List<BenchmarkResult> _results = [];
    private readonly Stopwatch _frameTimer = new();
    private readonly Queue<float> _frameTimes = new();
    private const int MaxFrameSamples = 120;

    private readonly Queue<float> _cpuUsageSamples = new();
    private readonly Queue<long> _memorySamples = new();
    private Process? _currentProcess;
    private DateTime _lastCpuCheck = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private float _currentCpuUsage;
    private long _currentMemoryUsageMB;

    private float _testElapsedTime;
    private int _frameCount;
    private List<BenchmarkResult> _baselineResults = [];
    private readonly Graphics2DStatsAggregator _statsAggregator = new();
    private readonly Queue<float> _flushMsHistory = new();
    private readonly Queue<float> _gpuQuadMsHistory = new();
    private readonly Queue<float> _colorCpuHistory = new();
    private readonly Queue<float> _shadowCpuHistory = new();
    private readonly float[] _flushMsPlotBuffer = new float[MaxFrameSamples];
    private readonly float[] _gpuQuadMsPlotBuffer = new float[MaxFrameSamples];

    private IBenchmarkRunner? _category;
    private string? _status;

    public void OnAttach()
    {
        _twoD.Load();
        _currentProcess = Process.GetCurrentProcess();
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _lastCpuCheck = DateTime.UtcNow;
    }

    public void OnDetach()
    {
        Finish(commit: false);
        _twoD.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _frameTimer.Restart();
        UpdateSystemMetrics();
        graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        graphics2D.Clear();

        var run = _active.Run;
        if (run != null)
        {
            run.Tick(timeSpan);
            _testElapsedTime += (float)timeSpan.TotalSeconds;
            _frameCount++;
            if (run.SamplesRenderer2D)
                RecordRendererStats(graphics2D.GetStats());
            else
                RecordPipelineStats(graphics3D.GetStats());
        }

        _frameTimer.Stop();
        RecordFrameTime((float)_frameTimer.Elapsed.TotalMilliseconds);

        if (run != null && _testElapsedTime >= run.DurationSeconds)
            Finish(commit: true);
    }

    public void Draw()
    {
        RenderBenchmarkUI();
        RenderResultsWindow();
        RenderPerformanceMonitor();
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        if (_active.Run is PipelineBenchmarkRun pipeline)
            pipeline.HandleInput(windowEvent, ImGui.GetIO().WantCaptureMouse);
        else if (_category is Benchmark2DRunner && windowEvent is MouseScrolledEvent scrollEvent)
            _twoD.AdjustZoom(scrollEvent.YOffset);
    }

    private void RenderBenchmarkUI()
    {
        ImGui.Begin("Benchmark Control", ImGuiWindowFlags.AlwaysVerticalScrollbar);

        if (_category == null)
        {
            ImGui.Text("Choose a category");
            ImGui.Separator();
            if (ImGui.Button("2D"))
                _category = _twoD;
            if (ImGui.Button("3D"))
                _category = _threeD;
            if (ImGui.Button("Lighting"))
                _category = _lighting;
            if (ImGui.Button("Shadows"))
                _category = _shadows;
            ImGui.End();
            return;
        }

        if (ImGui.Button("Back"))
        {
            Finish(commit: false);
            _category = null;
            _status = null;
            ImGui.End();
            return;
        }

        ImGui.Text(_category.Title);
        ImGui.Separator();

        var running = _active.Run != null;
        if (!running)
            _category.DrawControls();

        ImGui.Separator();
        if (!running)
        {
            if (_status != null)
                ImGui.TextWrapped(_status);

            foreach (var test in _category.Tests)
            {
                if (ImGui.Button(test.Label))
                    Start(test.Id);
            }
        }
        else
        {
            var duration = System.Math.Max(_active.Run!.DurationSeconds, 0.001f);
            ImGui.Text($"Running: {_active.Run.ResultName}");
            ImGui.Text($"Progress: {(_testElapsedTime / duration * 100):F1}%");
            ImGui.ProgressBar(_testElapsedTime / duration);
            if (ImGui.Button("Stop"))
                Finish(commit: true);
        }

        ImGui.End();
    }

    private void Start(string testId)
    {
        if (!_category!.TryCreate(testId, _category.Settings, out var run, out var error))
        {
            _status = error;
            return;
        }

        _status = null;
        _active.Adopt(run!);
        ResetSamples();
    }

    private void Finish(bool commit)
    {
        string? storedName = null;
        var stored = _active.Finish(commit, _frameTimes.Count, run =>
        {
            var result = BuildResult(run);
            storedName = result.TestName;
            _results.Add(result);
        });

        if (_active.Run != null)
        {
            ResetSamples();
            return;
        }

        if (stored && storedName == "Profiling2D_MultiTexture")
            ExportResultsToMarkdown();
    }

    private void ResetSamples()
    {
        _testElapsedTime = 0;
        _frameCount = 0;
        _frameTimes.Clear();
        _cpuUsageSamples.Clear();
        _memorySamples.Clear();
        _statsAggregator.Clear();
        _flushMsHistory.Clear();
        _gpuQuadMsHistory.Clear();
        _colorCpuHistory.Clear();
        _shadowCpuHistory.Clear();
    }

    private BenchmarkResult BuildResult(BenchmarkRun run)
    {
        var frameTimes = _frameTimes.ToArray();
        Array.Sort(frameTimes);
        var cpuSamples = _cpuUsageSamples.ToArray();
        var memorySamples = _memorySamples.ToArray();

        var result = new BenchmarkResult
        {
            TestName = run.ResultName,
            TotalFrames = _frameCount,
            AverageFrameTime = frameTimes.Average(),
            MinFPS = 1000.0f / frameTimes.Max(),
            MaxFPS = 1000.0f / frameTimes.Min(),
            AverageFPS = 1000.0f / frameTimes.Average(),
            Percentile99 = frameTimes[(int)(frameTimes.Length * 0.99)],
            TestDuration = _testElapsedTime,
            AverageCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Average() : 0,
            MaxCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Max() : 0,
            MinCpuUsage = cpuSamples.Length > 0 ? cpuSamples.Min() : 0,
            AverageMemoryUsageMB = memorySamples.Length > 0 ? (long)memorySamples.Average() : 0,
            MaxMemoryUsageMB = memorySamples.Length > 0 ? memorySamples.Max() : 0,
            MinMemoryUsageMB = memorySamples.Length > 0 ? memorySamples.Min() : 0
        };

        _statsAggregator.ApplyTo(result);
        run.Contribute(result);
        return result;
    }

    private void RenderResultsWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(DisplayConfig.StandardPopupSize.Width, 500), ImGuiCond.FirstUseEver);
        ImGui.Begin("Benchmark Results");

        if (_results.Count > 0)
        {
            if (ImGui.Button("Clear Results"))
                _results.Clear();
            if (ImGui.Button("Save as Baseline"))
                BenchmarkStorage.SaveBaseline(_results);
            ImGui.SameLine();
            if (ImGui.Button("Load Baseline"))
                _baselineResults = BenchmarkStorage.LoadBaseline();
            ImGui.SameLine();
            if (ImGui.Button("Export to Markdown"))
                ExportResultsToMarkdown();
            ImGui.Separator();

            foreach (var result in _results)
            {
                var baseline = _baselineResults.FirstOrDefault(b => b.TestName == result.TestName);
                RenderSingleResult(result, baseline);
            }
        }
        else
        {
            ImGui.Text("No benchmark results yet.");
        }

        ImGui.End();
    }

    private static void RenderSingleResult(BenchmarkResult result, BenchmarkResult? baseline)
    {
        ImGui.Text($"{result.TestName}:");
        ImGui.Indent();
        ImGui.Text("Performance:");
        ImGui.Indent();
        ImGui.Text($"Avg FPS: {result.AverageFPS:F2}");
        ImGui.Text($"Min FPS: {result.MinFPS:F2}");
        ImGui.Text($"Max FPS: {result.MaxFPS:F2}");
        ImGui.Text($"Avg Frame Time: {result.AverageFrameTime:F2}ms");
        ImGui.Text($"99th Percentile: {result.Percentile99:F2}ms");
        ImGui.Text($"Total Frames: {result.TotalFrames}");
        ImGui.Unindent();
        ImGui.Text("CPU Usage:");
        ImGui.Indent();
        ImGui.Text($"Average: {result.AverageCpuUsage:F2}%");
        ImGui.Text($"Min: {result.MinCpuUsage:F2}%");
        ImGui.Text($"Max: {result.MaxCpuUsage:F2}%");
        ImGui.Unindent();
        ImGui.Text("Memory Usage:");
        ImGui.Indent();
        ImGui.Text($"Average: {result.AverageMemoryUsageMB} MB");
        ImGui.Text($"Min: {result.MinMemoryUsageMB} MB");
        ImGui.Text($"Max: {result.MaxMemoryUsageMB} MB");
        ImGui.Unindent();
        if (result.CustomMetrics.Count > 0)
        {
            ImGui.Text("Custom Metrics:");
            ImGui.Indent();
            foreach (var metric in result.CustomMetrics)
                ImGui.Text($"{metric.Key}: {metric.Value}");
            ImGui.Unindent();
        }

        ImGui.Unindent();
        ImGui.Separator();
        if (baseline != null)
            RenderBaselineComparison(result, baseline);
    }

    private static void RenderBaselineComparison(BenchmarkResult result, BenchmarkResult baseline)
    {
        ImGui.Text("Comparison with Baseline:");
        ImGui.Indent();
        var fpsDiff = result.AverageFPS - baseline.AverageFPS;
        ImGui.PushStyleColor(ImGuiCol.Text, fpsDiff >= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
        ImGui.Text($"Δ Avg FPS: {fpsDiff:+0.00;-0.00;0.00}");
        ImGui.PopStyleColor();
        var frameTimeDiff = result.AverageFrameTime - baseline.AverageFrameTime;
        ImGui.PushStyleColor(ImGuiCol.Text, frameTimeDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
        ImGui.Text($"Δ Frame Time: {frameTimeDiff:+0.00;-0.00;0.00}ms");
        ImGui.PopStyleColor();
        var cpuDiff = result.AverageCpuUsage - baseline.AverageCpuUsage;
        ImGui.PushStyleColor(ImGuiCol.Text, cpuDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
        ImGui.Text($"Δ Avg CPU: {cpuDiff:+0.00;-0.00;0.00}%");
        ImGui.PopStyleColor();
        var memoryDiff = result.AverageMemoryUsageMB - baseline.AverageMemoryUsageMB;
        ImGui.PushStyleColor(ImGuiCol.Text, memoryDiff <= 0 ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
        ImGui.Text($"Δ Avg Memory: {memoryDiff:+0;-0;0} MB");
        ImGui.PopStyleColor();
        ImGui.Unindent();
    }

    private void RenderPerformanceMonitor()
    {
        ImGui.Begin("Performance Monitor##Benchmark");
        var frameTimes = _frameTimes.ToArray();
        if (frameTimes.Length > 0)
        {
            var avgFrameTime = frameTimes.Average();
            var minFrameTime = frameTimes.Min();
            var maxFrameTime = frameTimes.Max();
            ImGui.Text("Performance:");
            ImGui.Indent();
            ImGui.Text($"Current FPS: {(1000.0f / avgFrameTime):F2}");
            ImGui.Text($"Frame Time: {avgFrameTime:F2}ms (min: {minFrameTime:F2}, max: {maxFrameTime:F2})");
            if (frameTimes.Length > 1)
            {
                ImGui.PlotLines("Frame Times", ref frameTimes[0], frameTimes.Length, 0,
                    null, 0, maxFrameTime * 1.2f, new Vector2(0, 80));
            }

            ImGui.Unindent();
        }
        else
        {
            ImGui.Text("Collecting performance data...");
        }

        ImGui.Separator();
        ImGui.Text("System Resources:");
        ImGui.Indent();
        ImGui.Text($"CPU Usage: {_currentCpuUsage:F2}%");
        ImGui.Text($"RAM Usage: {_currentMemoryUsageMB} MB");
        ImGui.Text($"CPU Cores: {Environment.ProcessorCount}");
        ImGui.Unindent();

        ImGui.Separator();
        switch (_category?.Title)
        {
            case "2D":
                Draw2DStats();
                break;
            case "3D":
                Draw3DStats(graphics3D.GetStats());
                break;
            case "Lighting":
                DrawLightingStats(graphics3D.GetStats());
                break;
            case "Shadows":
                DrawShadowStats(graphics3D.GetStats());
                break;
        }

        ImGui.End();
    }

    private void Draw2DStats()
    {
        var stats2D = graphics2D.GetStats();
        ImGui.Text("Renderer2D");
        ImGui.Indent();
        ImGui.Text($"Quad Draw Calls: {stats2D.DrawCalls}");
        ImGui.Text($"Line Draw Calls: {stats2D.LineDrawCalls}");
        ImGui.Text($"Quads: {stats2D.QuadCount}");
        ImGui.Text($"Line Vertices: {stats2D.LineVertexCount}");
        ImGui.Text($"Vertices: {stats2D.GetTotalVertexCount()}");
        ImGui.Text($"Batch Count: {stats2D.BatchCount}");
        ImGui.Text($"Texture Binds: {stats2D.TextureBinds}");
        ImGui.Text($"Program Switches: {stats2D.ProgramSwitches}");
        ImGui.Text($"Upload: {stats2D.UploadBytes / 1024.0:F1} KB");
        ImGui.Text($"CPU BatchFill: {stats2D.BatchFillMs:F3} ms");
        ImGui.Text($"CPU Flush: {stats2D.FlushMs:F3} ms");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("GPU times are from the previous frame (timer query lag).");
        ImGui.Text($"GPU Quad Pass: {stats2D.GpuQuadPassMs:F3} ms");
        ImGui.Text($"GPU Line Pass: {stats2D.GpuLinePassMs:F3} ms");
        PlotHistory("Flush Ms", _flushMsHistory, _flushMsPlotBuffer);
        PlotHistory("GPU Quad Ms (lag 1f)", _gpuQuadMsHistory, _gpuQuadMsPlotBuffer);
        ImGui.Unindent();
    }

    private void Draw3DStats(Statistics stats)
    {
        ImGui.Text("Renderer3D");
        ImGui.Indent();
        ImGui.Text($"Color Draw Calls: {stats.ColorDrawCalls}");
        ImGui.Text($"Cubes: {stats.CubeDraws}");
        ImGui.Text($"Mesh Draws: {stats.MeshDraws} (instanced: {stats.InstancedDraws})");
        ImGui.Text($"Instances: {stats.Instances}");
        ImGui.Text($"Vertices: {stats.Vertices}");
        ImGui.Text($"Triangles: {stats.Triangles}");
        ImGui.Text($"CPU Color: {stats.ColorCpuMs:F3} ms");
        ImGui.Text($"Directional Shadow: {(stats.DirectionalShadow ? "on" : "off")}");
        ImGui.Text($"Point Shadow Lights: {stats.PointShadowLights}");
        PlotHistory("CPU Color Ms", _colorCpuHistory, _flushMsPlotBuffer);
        ImGui.Unindent();
    }

    private void DrawLightingStats(Statistics stats)
    {
        ImGui.Text("Lighting");
        ImGui.Indent();
        ImGui.Text($"Point Lights: {stats.PointLights}");
        ImGui.Text($"Directional Shadow: {(stats.DirectionalShadow ? "on" : "off")}");
        ImGui.Text($"Point Shadow Lights: {stats.PointShadowLights}");
        ImGui.Text($"Color Draw Calls: {stats.ColorDrawCalls}");
        ImGui.Text($"CPU Color: {stats.ColorCpuMs:F3} ms");
        ImGui.Text($"CPU Shadows: {stats.ShadowCpuMs:F3} ms");
        PlotHistory("CPU Color Ms", _colorCpuHistory, _flushMsPlotBuffer);
        ImGui.Unindent();
    }

    private void DrawShadowStats(Statistics stats)
    {
        ImGui.Text("Shadows");
        ImGui.Indent();
        ImGui.Text($"Directional Shadow: {(stats.DirectionalShadow ? "on" : "off")}");
        ImGui.Text($"Dir Shadow Draw Calls: {stats.DirectionalShadowDrawCalls}");
        ImGui.Text($"Point Lights: {stats.PointLights} (shadow: {stats.PointShadowLights}, cache hits: {stats.PointShadowCacheHits})");
        ImGui.Text($"Point Shadow Draw Calls: {stats.PointShadowDrawCalls}");
        ImGui.Text($"Shadow Caster Culled: {stats.ShadowCasterCulled}");
        ImGui.Text($"CPU Shadows: {stats.ShadowCpuMs:F3} ms");
        ImGui.Text($"CPU Color: {stats.ColorCpuMs:F3} ms");
        ImGui.Text($"Color Draw Calls: {stats.ColorDrawCalls}");
        PlotHistory("CPU Shadow Ms", _shadowCpuHistory, _flushMsPlotBuffer);
        PlotHistory("CPU Color Ms", _colorCpuHistory, _gpuQuadMsPlotBuffer);
        ImGui.Unindent();
    }

    private static void PlotHistory(string label, Queue<float> history, float[] buffer)
    {
        if (history.Count <= 1)
            return;

        CopyQueueToPlotBuffer(history, buffer);
        var max = 0f;
        for (var i = 0; i < history.Count; i++)
            if (buffer[i] > max)
                max = buffer[i];
        ImGui.PlotLines(label, ref buffer[0], history.Count, 0, null, 0, System.Math.Max(max * 1.2f, 0.01f), new Vector2(0, 60));
    }

    private void RecordRendererStats(Graphics2DStats stats)
    {
        _statsAggregator.AddSample(stats);
        EnqueueSample(_flushMsHistory, (float)stats.FlushMs);
        EnqueueSample(_gpuQuadMsHistory, (float)stats.GpuQuadPassMs);
    }

    private void RecordPipelineStats(Statistics stats)
    {
        EnqueueSample(_colorCpuHistory, (float)stats.ColorCpuMs);
        EnqueueSample(_shadowCpuHistory, (float)stats.ShadowCpuMs);
    }

    private static void EnqueueSample(Queue<float> history, float sample)
    {
        history.Enqueue(sample);
        if (history.Count > MaxFrameSamples)
            history.Dequeue();
    }

    private static void CopyQueueToPlotBuffer(Queue<float> source, float[] buffer)
    {
        var i = 0;
        foreach (var value in source)
            buffer[i++] = value;
    }

    private void RecordFrameTime(float frameTimeMs)
    {
        _frameTimes.Enqueue(frameTimeMs);
        if (_frameTimes.Count > MaxFrameSamples)
            _frameTimes.Dequeue();
    }

    private void UpdateSystemMetrics()
    {
        if (_currentProcess == null)
            return;

        try
        {
            _currentProcess.Refresh();
            var currentTime = DateTime.UtcNow;
            var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
            var timeDiff = (currentTime - _lastCpuCheck).TotalMilliseconds;
            if (timeDiff > 500)
            {
                var cpuTimeDiff = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                var cpuUsagePercent = (float)((cpuTimeDiff / (Environment.ProcessorCount * timeDiff)) * 100.0);
                _currentCpuUsage = System.Math.Clamp(cpuUsagePercent, 0, 100 * Environment.ProcessorCount);
                _lastCpuCheck = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;
            }

            _currentMemoryUsageMB = _currentProcess.WorkingSet64 / (1024 * 1024);
            if (_active.Run != null)
            {
                _cpuUsageSamples.Enqueue(_currentCpuUsage);
                _memorySamples.Enqueue(_currentMemoryUsageMB);
                if (_cpuUsageSamples.Count > 1000)
                    _cpuUsageSamples.Dequeue();
                if (_memorySamples.Count > 1000)
                    _memorySamples.Dequeue();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update system metrics: {ex.Message}");
        }
    }

    private void ExportResultsToMarkdown()
    {
        try
        {
            var markdown = new System.Text.StringBuilder();
            markdown.AppendLine("# Benchmark Results");
            markdown.AppendLine();
            markdown.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"**Platform:** {Environment.OSVersion.Platform}");
            markdown.AppendLine($"**CPU Cores:** {Environment.ProcessorCount}");
            markdown.AppendLine();
            foreach (var result in _results)
            {
                AppendResultSection(markdown, result);
                var baseline = _baselineResults.FirstOrDefault(b => b.TestName == result.TestName);
                if (baseline != null)
                    AppendBaselineSection(markdown, result, baseline);
                markdown.AppendLine("---");
                markdown.AppendLine();
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"benchmark_results_{timestamp}.md";
            File.WriteAllText(filename, markdown.ToString());
            Console.WriteLine($"Benchmark results exported to: {Path.GetFullPath(filename)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to export results to Markdown: {ex.Message}");
        }
    }

    private static void AppendResultSection(System.Text.StringBuilder markdown, BenchmarkResult result)
    {
        markdown.AppendLine($"## {result.TestName}");
        markdown.AppendLine();
        markdown.AppendLine("### Performance");
        markdown.AppendLine("| Metric | Value |");
        markdown.AppendLine("|--------|-------|");
        markdown.AppendLine($"| Average FPS | {result.AverageFPS:F2} |");
        markdown.AppendLine($"| Min FPS | {result.MinFPS:F2} |");
        markdown.AppendLine($"| Max FPS | {result.MaxFPS:F2} |");
        markdown.AppendLine($"| Average Frame Time | {result.AverageFrameTime:F2} ms |");
        markdown.AppendLine($"| 99th Percentile | {result.Percentile99:F2} ms |");
        markdown.AppendLine($"| Total Frames | {result.TotalFrames} |");
        markdown.AppendLine($"| Test Duration | {result.TestDuration:F2}s |");
        markdown.AppendLine();
        markdown.AppendLine("### CPU Usage");
        markdown.AppendLine("| Metric | Value |");
        markdown.AppendLine("|--------|-------|");
        markdown.AppendLine($"| Average | {result.AverageCpuUsage:F2}% |");
        markdown.AppendLine($"| Min | {result.MinCpuUsage:F2}% |");
        markdown.AppendLine($"| Max | {result.MaxCpuUsage:F2}% |");
        markdown.AppendLine();
        markdown.AppendLine("### Memory Usage");
        markdown.AppendLine("| Metric | Value |");
        markdown.AppendLine("|--------|-------|");
        markdown.AppendLine($"| Average | {result.AverageMemoryUsageMB} MB |");
        markdown.AppendLine($"| Min | {result.MinMemoryUsageMB} MB |");
        markdown.AppendLine($"| Max | {result.MaxMemoryUsageMB} MB |");
        markdown.AppendLine();
        if (result.CustomMetrics.Count > 0)
        {
            markdown.AppendLine("### Custom Metrics");
            markdown.AppendLine("| Metric | Value |");
            markdown.AppendLine("|--------|-------|");
            foreach (var metric in result.CustomMetrics)
                markdown.AppendLine($"| {metric.Key} | {metric.Value} |");
            markdown.AppendLine();
        }
    }

    private static void AppendBaselineSection(System.Text.StringBuilder markdown, BenchmarkResult result, BenchmarkResult baseline)
    {
        markdown.AppendLine("### Comparison with Baseline");
        markdown.AppendLine("| Metric | Delta | Status |");
        markdown.AppendLine("|--------|-------|--------|");
        var fpsDiff = result.AverageFPS - baseline.AverageFPS;
        markdown.AppendLine($"| Avg FPS | {fpsDiff:+0.00;-0.00;0.00} | {(fpsDiff >= 0 ? "🟢" : "🔴")} |");
        var frameTimeDiff = result.AverageFrameTime - baseline.AverageFrameTime;
        markdown.AppendLine($"| Avg Frame Time | {frameTimeDiff:+0.00;-0.00;0.00} ms | {(frameTimeDiff <= 0 ? "🟢" : "🔴")} |");
        var cpuDiff = result.AverageCpuUsage - baseline.AverageCpuUsage;
        markdown.AppendLine($"| Avg CPU | {cpuDiff:+0.00;-0.00;0.00}% | {(cpuDiff <= 0 ? "🟢" : "🔴")} |");
        var memoryDiff = result.AverageMemoryUsageMB - baseline.AverageMemoryUsageMB;
        markdown.AppendLine($"| Avg Memory | {memoryDiff:+0;-0;0} MB | {(memoryDiff <= 0 ? "🟢" : "🔴")} |");
        markdown.AppendLine();
    }
}
