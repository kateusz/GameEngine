using System.Diagnostics;
using Engine.Core;
using Engine.Platform.SilkNet;
using Engine.Renderer.Pipeline;
using Serilog;

namespace Benchmark.Cli;

public sealed class CliBenchmarkLayer(
    PerfRunOptions options,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    BenchmarkRunnerFactory runners) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<CliBenchmarkLayer>();

    private readonly Stopwatch _wall = new();
    private readonly Stopwatch _frameTimer = new();
    private readonly BenchmarkProcessSampler _sampler = new();
    private readonly Graphics2DStatsAggregator _stats2D = new();
    private readonly PipelineStatsAggregator _stats3D = new();
    private readonly List<float> _frameTimes = new();
    private readonly List<BenchmarkResult> _iterationResults = new();
    private readonly List<float> _allFrameTimes = new();

    private BenchmarkRun? _run;
    private float _phaseSeconds;
    private float _totalMeasuredSeconds;
    private bool _warmingUp;
    private bool _measuring;
    private bool _done;
    private int _exitCode;
    private int _measureIteration;

    public int ExitCode => _exitCode;

    public void OnAttach()
    {
        _wall.Start();
        _sampler.Start();
        Environment.SetEnvironmentVariable("BENCHMARK_SEED", options.Seed.ToString());

        runners.Prepare(options.Category);
        var runner = runners.Create(options.Category);
        if (runner == null)
        {
            Fail(2, $"Unknown category: {options.Category}");
            return;
        }

        if (runner is Benchmark3DRunner threeD)
            threeD.SetCliScenePath(options.SceneFile);

        var totalMeasure = options.MeasureSeconds * options.MeasureIterations;
        var settings = new BenchmarkSettings(options.Count, options.WarmupSeconds + totalMeasure);
        if (!runner.TryCreate(options.TestId, settings, out var run, out var createError) || run == null)
        {
            Fail(2, createError ?? "Failed to create benchmark run");
            return;
        }

        _run = run;
        _warmingUp = options.WarmupSeconds > 0f;
        _measuring = options.WarmupSeconds <= 0f;
        _phaseSeconds = 0f;
        _measureIteration = 0;
        Logger.Information("Perf run {Category}:{Test} warmup={Warmup}s measure={Measure}s x{Iterations}",
            options.Category, options.TestId, options.WarmupSeconds, options.MeasureSeconds, options.MeasureIterations);
    }

    public void OnDetach() => runners.Dispose2D();

    public void OnUpdate(TimeSpan delta)
    {
        if (_done || _run == null)
            return;

        if (_wall.Elapsed.TotalSeconds > options.StartupTimeoutSeconds)
        {
            Fail(3, $"Timeout ({options.StartupTimeoutSeconds}s)");
            return;
        }

        _frameTimer.Restart();
        _run.Tick(delta);
        if (_measuring)
        {
            _sampler.SampleDuringMeasure();
            if (_run.SamplesRenderer2D)
                _stats2D.AddSample(graphics2D.GetStats());
            else
                _stats3D.AddSample(graphics3D.GetStats());
        }

        _frameTimer.Stop();
        _phaseSeconds += (float)delta.TotalSeconds;

        if (_warmingUp)
        {
            if (_phaseSeconds >= options.WarmupSeconds)
            {
                _warmingUp = false;
                _measuring = true;
                _phaseSeconds = 0f;
                _frameTimes.Clear();
                _sampler.Clear();
                _stats2D.Clear();
                _stats3D.Clear();
            }

            return;
        }

        if (_measuring)
        {
            _frameTimes.Add((float)_frameTimer.Elapsed.TotalMilliseconds);
            if (_phaseSeconds >= options.MeasureSeconds)
                CompleteMeasureIteration();
        }
    }

    public void Draw() { }

    public void HandleInputEvent(Engine.Events.Input.InputEvent windowEvent) { }

    private void CompleteMeasureIteration()
    {
        var cpu = _sampler.CpuSamples;
        var mem = _sampler.MemSamples;
        var iteration = BenchmarkResultBuilder.Build(
            _run!,
            _frameTimes,
            _phaseSeconds,
            _frameTimes.Count,
            cpu.Count > 0 ? (float)cpu.Average() : 0,
            cpu.Count > 0 ? cpu.Max() : 0,
            cpu.Count > 0 ? cpu.Min() : 0,
            mem.Count > 0 ? (long)mem.Average() : 0,
            mem.Count > 0 ? mem.Max() : 0,
            mem.Count > 0 ? mem.Min() : 0,
            _run!.SamplesRenderer2D ? _stats2D : null,
            _run.SamplesRenderer2D ? null : _stats3D);
        _iterationResults.Add(iteration);
        _allFrameTimes.AddRange(_frameTimes);
        _totalMeasuredSeconds += _phaseSeconds;
        _measureIteration++;

        if (_measureIteration < options.MeasureIterations)
        {
            _phaseSeconds = 0f;
            _frameTimes.Clear();
            _sampler.Clear();
            _stats2D.Clear();
            _stats3D.Clear();
            return;
        }

        FinishSuccess();
    }

    private void FinishSuccess()
    {
        if (_done)
            return;
        _done = true;

        var iterationFps = _iterationResults.ConvertAll(r => r.AverageFPS);
        var iterationFrameMs = _iterationResults.ConvertAll(r => r.AverageFrameTime);
        var medianFps = BenchmarkResultBuilder.Median(iterationFps);
        var medianFrameMs = BenchmarkResultBuilder.Median(iterationFrameMs);

        var last = _iterationResults[^1];
        var result = BenchmarkResultBuilder.Build(
            _run!,
            _allFrameTimes,
            _totalMeasuredSeconds,
            _allFrameTimes.Count,
            last.AverageCpuUsage,
            last.MaxCpuUsage,
            last.MinCpuUsage,
            last.AverageMemoryUsageMB,
            last.MaxMemoryUsageMB,
            last.MinMemoryUsageMB,
            null,
            null) with
        {
            AverageFPS = medianFps,
            AverageFrameTime = medianFrameMs
        };
        _run!.Contribute(result);
        foreach (var (key, value) in last.CustomMetrics)
        {
            if (key is "Median Frame Time Ms" or "P95 Frame Time Ms")
                continue;
            result.CustomMetrics.TryAdd(key, value);
        }

        result.CustomMetrics["Measure iterations"] = options.MeasureIterations.ToString();
        for (var i = 0; i < _iterationResults.Count; i++)
        {
            var iter = _iterationResults[i];
            result.CustomMetrics[$"Iteration {i + 1} Avg FPS"] =
                iter.AverageFPS.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            result.CustomMetrics[$"Iteration {i + 1} Avg Frame Ms"] =
                iter.AverageFrameTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        }

        var framesCsv = Path.ChangeExtension(options.OutputPath, ".frames.csv");
        PerfMetricsWriter.WriteFrameCsv(framesCsv, _allFrameTimes);
        var ok = _allFrameTimes.Count > 0;
        PerfMetricsWriter.WriteJson(options.OutputPath, result, options, ok,
            ok ? null : "No frames in measure window",
            _run.SamplesRenderer2D ? "2d" : "3d", framesCsv,
            iterationFps, iterationFrameMs);
        _exitCode = ok ? 0 : 2;
        RequestClose();
    }

    private void Fail(int code, string message)
    {
        if (_done)
            return;
        _done = true;
        _exitCode = code;
        PerfMetricsWriter.WriteJson(options.OutputPath,
            new BenchmarkResult { TestName = $"{options.Category}:{options.TestId}" },
            options, false, message, "", null, null, null);
        Logger.Error(message);
        RequestClose();
    }

    private static void RequestClose() => SilkNetContext.Window?.Close();
}
