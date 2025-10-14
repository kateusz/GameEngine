namespace Benchmark;

/// <summary>
/// Configuration for automated benchmark execution
/// </summary>
public class BenchmarkConfig
{
    /// <summary>
    /// Run in headless mode (no GUI)
    /// </summary>
    public bool Headless { get; set; }

    /// <summary>
    /// Which tests to run (comma-separated or "all")
    /// </summary>
    public string Tests { get; set; } = "all";

    /// <summary>
    /// Path to baseline results file for comparison
    /// </summary>
    public string? BaselinePath { get; set; }

    /// <summary>
    /// Output file path for results (JSON)
    /// </summary>
    public string OutputPath { get; set; } = "benchmark-results.json";

    /// <summary>
    /// Performance regression threshold percentage (e.g., 10 for 10%)
    /// </summary>
    public float RegressionThreshold { get; set; } = 10.0f;

    /// <summary>
    /// Duration for each test in seconds
    /// </summary>
    public float TestDuration { get; set; } = 5.0f;

    /// <summary>
    /// Entity count for stress tests
    /// </summary>
    public int EntityCount { get; set; } = 1000;

    /// <summary>
    /// Enable verbose output
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Fail (exit code 1) if regression detected
    /// </summary>
    public bool FailOnRegression { get; set; } = true;

    /// <summary>
    /// Parse command line arguments into BenchmarkConfig
    /// </summary>
    public static BenchmarkConfig ParseArgs(string[] args)
    {
        var config = new BenchmarkConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--headless":
                case "-h":
                    config.Headless = true;
                    break;

                case "--tests":
                case "-t":
                    if (i + 1 < args.Length)
                        config.Tests = args[++i];
                    break;

                case "--baseline":
                case "-b":
                    if (i + 1 < args.Length)
                        config.BaselinePath = args[++i];
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        config.OutputPath = args[++i];
                    break;

                case "--threshold":
                    if (i + 1 < args.Length && float.TryParse(args[++i], out var threshold))
                        config.RegressionThreshold = threshold;
                    break;

                case "--duration":
                case "-d":
                    if (i + 1 < args.Length && float.TryParse(args[++i], out var duration))
                        config.TestDuration = duration;
                    break;

                case "--entities":
                case "-e":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var entities))
                        config.EntityCount = entities;
                    break;

                case "--verbose":
                case "-v":
                    config.Verbose = true;
                    break;

                case "--no-fail-on-regression":
                    config.FailOnRegression = false;
                    break;

                case "--help":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return config;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Game Engine Benchmark Tool
===========================

Usage: dotnet run --project Benchmark -- [OPTIONS]

Options:
  --headless, -h              Run in headless mode (no GUI)
  --tests, -t <tests>         Tests to run (comma-separated or 'all')
                              Available: Renderer2DStress, TextureSwitching, DrawCallOptimization
  --baseline, -b <path>       Path to baseline results for comparison
  --output, -o <path>         Output file path (default: benchmark-results.json)
  --threshold <percent>       Regression threshold percentage (default: 10)
  --duration, -d <seconds>    Test duration in seconds (default: 5)
  --entities, -e <count>      Entity count for stress tests (default: 1000)
  --verbose, -v               Enable verbose output
  --no-fail-on-regression     Don't exit with error code on regression
  --help                      Show this help message

Examples:
  # Run all benchmarks in headless mode
  dotnet run --project Benchmark -- --headless --tests all

  # Run specific test and compare with baseline
  dotnet run --project Benchmark -- --headless --tests Renderer2DStress --baseline baseline.json

  # Run with custom settings
  dotnet run --project Benchmark -- --headless --duration 10 --entities 5000 --threshold 5

Exit Codes:
  0 - Success (no regressions or regression check disabled)
  1 - Performance regression detected
  2 - Error during execution
");
    }

    /// <summary>
    /// Get list of tests to run
    /// </summary>
    public List<BenchmarkTestType> GetTestsToRun()
    {
        var tests = new List<BenchmarkTestType>();

        if (Tests.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            tests.Add(BenchmarkTestType.Renderer2DStress);
            tests.Add(BenchmarkTestType.TextureSwitching);
            tests.Add(BenchmarkTestType.DrawCallOptimization);
        }
        else
        {
            var testNames = Tests.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var testName in testNames)
            {
                if (Enum.TryParse<BenchmarkTestType>(testName.Trim(), true, out var testType)
                    && testType != BenchmarkTestType.None)
                {
                    tests.Add(testType);
                }
                else
                {
                    Console.WriteLine($"Warning: Unknown test type '{testName}', skipping...");
                }
            }
        }

        return tests;
    }
}
