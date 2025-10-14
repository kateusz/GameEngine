namespace Benchmark;

/// <summary>
/// Detects performance regressions by comparing benchmark results
/// </summary>
public class RegressionDetector
{
    public class RegressionAnalysis
    {
        public bool HasRegressions { get; set; }
        public List<RegressionDetail> Regressions { get; set; } = new();
        public List<ImprovementDetail> Improvements { get; set; } = new();

        public void PrintSummary(bool verbose = false)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("BENCHMARK COMPARISON RESULTS");
            Console.WriteLine(new string('=', 60));

            if (Regressions.Count == 0 && Improvements.Count == 0)
            {
                Console.WriteLine("No baseline data for comparison.");
                return;
            }

            // Print improvements
            if (Improvements.Count > 0)
            {
                Console.WriteLine("\n✓ IMPROVEMENTS:");
                foreach (var improvement in Improvements)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  {improvement.TestName}:");
                    Console.WriteLine($"    FPS:        {improvement.FpsDelta:+0.00;-0.00} ({improvement.FpsPercentChange:+0.0;-0.0}%)");
                    Console.WriteLine($"    Frame Time: {improvement.FrameTimeDelta:+0.00;-0.00}ms ({improvement.FrameTimePercentChange:+0.0;-0.0}%)");
                    Console.ResetColor();

                    if (verbose)
                    {
                        Console.WriteLine($"    Baseline: {improvement.BaselineAvgFPS:F2} FPS, {improvement.BaselineAvgFrameTime:F2}ms");
                        Console.WriteLine($"    Current:  {improvement.CurrentAvgFPS:F2} FPS, {improvement.CurrentAvgFrameTime:F2}ms");
                    }
                }
            }

            // Print regressions
            if (Regressions.Count > 0)
            {
                Console.WriteLine("\n✗ REGRESSIONS DETECTED:");
                foreach (var regression in Regressions)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  {regression.TestName}:");
                    Console.WriteLine($"    FPS:        {regression.FpsDelta:+0.00;-0.00} ({regression.FpsPercentChange:+0.0;-0.0}%)");
                    Console.WriteLine($"    Frame Time: {regression.FrameTimeDelta:+0.00;-0.00}ms ({regression.FrameTimePercentChange:+0.0;-0.0}%)");
                    Console.ResetColor();

                    if (verbose)
                    {
                        Console.WriteLine($"    Baseline: {regression.BaselineAvgFPS:F2} FPS, {regression.BaselineAvgFrameTime:F2}ms");
                        Console.WriteLine($"    Current:  {regression.CurrentAvgFPS:F2} FPS, {regression.CurrentAvgFrameTime:F2}ms");
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    Threshold exceeded: {regression.ThresholdExceeded:F1}%");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\n" + new string('=', 60));

            if (HasRegressions)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("RESULT: REGRESSION DETECTED ✗");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("RESULT: ALL TESTS PASSED ✓");
                Console.ResetColor();
            }

            Console.WriteLine(new string('=', 60) + "\n");
        }
    }

    public class RegressionDetail
    {
        public string TestName { get; set; } = string.Empty;
        public float BaselineAvgFPS { get; set; }
        public float CurrentAvgFPS { get; set; }
        public float FpsDelta { get; set; }
        public float FpsPercentChange { get; set; }
        public float BaselineAvgFrameTime { get; set; }
        public float CurrentAvgFrameTime { get; set; }
        public float FrameTimeDelta { get; set; }
        public float FrameTimePercentChange { get; set; }
        public float ThresholdExceeded { get; set; }
    }

    public class ImprovementDetail
    {
        public string TestName { get; set; } = string.Empty;
        public float BaselineAvgFPS { get; set; }
        public float CurrentAvgFPS { get; set; }
        public float FpsDelta { get; set; }
        public float FpsPercentChange { get; set; }
        public float BaselineAvgFrameTime { get; set; }
        public float CurrentAvgFrameTime { get; set; }
        public float FrameTimeDelta { get; set; }
        public float FrameTimePercentChange { get; set; }
    }

    private readonly float _regressionThreshold;

    public RegressionDetector(float regressionThresholdPercent)
    {
        _regressionThreshold = regressionThresholdPercent;
    }

    /// <summary>
    /// Analyze benchmark results against baseline
    /// </summary>
    public RegressionAnalysis Analyze(List<BenchmarkResult> current, List<BenchmarkResult> baseline)
    {
        var analysis = new RegressionAnalysis();

        foreach (var currentResult in current)
        {
            var baselineResult = baseline.FirstOrDefault(b => b.TestName == currentResult.TestName);
            if (baselineResult == null)
            {
                Console.WriteLine($"Warning: No baseline found for test '{currentResult.TestName}'");
                continue;
            }

            var fpsDelta = currentResult.AverageFPS - baselineResult.AverageFPS;
            var fpsPercentChange = (fpsDelta / baselineResult.AverageFPS) * 100;

            var frameTimeDelta = currentResult.AverageFrameTime - baselineResult.AverageFrameTime;
            var frameTimePercentChange = (frameTimeDelta / baselineResult.AverageFrameTime) * 100;

            // Check for regression (FPS decreased OR frame time increased beyond threshold)
            var fpsRegression = fpsPercentChange < -_regressionThreshold;
            var frameTimeRegression = frameTimePercentChange > _regressionThreshold;

            if (fpsRegression || frameTimeRegression)
            {
                analysis.HasRegressions = true;
                analysis.Regressions.Add(new RegressionDetail
                {
                    TestName = currentResult.TestName,
                    BaselineAvgFPS = baselineResult.AverageFPS,
                    CurrentAvgFPS = currentResult.AverageFPS,
                    FpsDelta = fpsDelta,
                    FpsPercentChange = fpsPercentChange,
                    BaselineAvgFrameTime = baselineResult.AverageFrameTime,
                    CurrentAvgFrameTime = currentResult.AverageFrameTime,
                    FrameTimeDelta = frameTimeDelta,
                    FrameTimePercentChange = frameTimePercentChange,
                    ThresholdExceeded = Math.Max(Math.Abs(fpsPercentChange), Math.Abs(frameTimePercentChange))
                });
            }
            else if (fpsDelta > 0 || frameTimeDelta < 0)
            {
                // Performance improvement
                analysis.Improvements.Add(new ImprovementDetail
                {
                    TestName = currentResult.TestName,
                    BaselineAvgFPS = baselineResult.AverageFPS,
                    CurrentAvgFPS = currentResult.AverageFPS,
                    FpsDelta = fpsDelta,
                    FpsPercentChange = fpsPercentChange,
                    BaselineAvgFrameTime = baselineResult.AverageFrameTime,
                    CurrentAvgFrameTime = currentResult.AverageFrameTime,
                    FrameTimeDelta = frameTimeDelta,
                    FrameTimePercentChange = frameTimePercentChange
                });
            }
        }

        return analysis;
    }
}
