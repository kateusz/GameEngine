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

    private BenchmarkRun? _run;
    private float _phaseSeconds;
    private bool _warmingUp;
    private bool _measuring;
    private bool _done;
    private int _exitCode;

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

        var settings = new BenchmarkSettings(options.Count, options.WarmupSeconds + options.MeasureSeconds);
        if (!runner.TryCreate(options.TestId, settings, out var run, out var createError) || run == null)
        {
            Fail(2, createError ?? "Failed to create benchmark run");
            return;
        }

        _run = run;
        _warmingUp = options.WarmupSeconds > 0f;
        _measuring = options.WarmupSeconds <= 0f;
        _phaseSeconds = 0f;
        Logger.Information("Perf run {Category}:{Test}", options.Category, options.TestId);
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
                FinishSuccess();
        }
    }

    public void Draw() { }

    public void HandleInputEvent(Engine.Events.Input.InputEvent windowEvent) { }

    private void FinishSuccess()
    {
        if (_done)
            return;
        _done = true;

        var cpu = _sampler.CpuSamples;
        var mem = _sampler.MemSamples;
        var result = BenchmarkResultBuilder.Build(
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

        var framesCsv = Path.ChangeExtension(options.OutputPath, ".frames.csv");
        PerfMetricsWriter.WriteFrameCsv(framesCsv, _frameTimes);
        var ok = _frameTimes.Count > 0;
        PerfMetricsWriter.WriteJson(options.OutputPath, result, options, ok,
            ok ? null : "No frames in measure window",
            _run.SamplesRenderer2D ? "2d" : "3d", framesCsv);
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
            options, false, message, "", null);
        Logger.Error(message);
        RequestClose();
    }

    private static void RequestClose() => SilkNetContext.Window?.Close();
}
