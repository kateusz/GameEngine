using System.Globalization;

namespace Benchmark.Cli;

public sealed record PerfRunOptions(
    string Category,
    string TestId,
    int Count,
    int Width,
    int Height,
    float WarmupSeconds,
    float MeasureSeconds,
    int MeasureIterations,
    int Seed,
    int TargetFrameRate,
    string OutputPath,
    float StartupTimeoutSeconds,
    string? ProjectRoot,
    string? SceneFile)
{
    public const string CliFlag = "--perf-run";

    public static bool TryParse(string[] args, out PerfRunOptions? options, out string? error)
    {
        options = null;
        error = null;
        if (args.Length == 0 || !args.Contains(CliFlag, StringComparer.Ordinal))
            return false;

        var category = "3D";
        var testId = "InstancedMesh";
        var count = 1000;
        var width = 1280;
        var height = 720;
        var warmup = 30f;
        var measure = 5f;
        var measureIterations = 5;
        var seed = 12345;
        var fps = 0;
        var output = "";
        var startupTimeout = 120f;
        string? scene = null;
        string? projectRoot = null;
        string? sceneFile = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == CliFlag)
                continue;

            if (TryReadValue(args, ref i, out var value))
            {
                switch (arg)
                {
                    case "--scene":
                        scene = value;
                        break;
                    case "--category":
                        category = value;
                        break;
                    case "--test":
                        testId = value;
                        break;
                    case "--count":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                        {
                            error = $"Invalid --count: {value}";
                            return false;
                        }

                        break;
                    case "--width":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out width))
                        {
                            error = $"Invalid --width: {value}";
                            return false;
                        }

                        break;
                    case "--height":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out height))
                        {
                            error = $"Invalid --height: {value}";
                            return false;
                        }

                        break;
                    case "--warmup-seconds":
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out warmup))
                        {
                            error = $"Invalid --warmup-seconds: {value}";
                            return false;
                        }

                        break;
                    case "--measure-seconds":
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out measure))
                        {
                            error = $"Invalid --measure-seconds: {value}";
                            return false;
                        }

                        break;
                    case "--measure-iterations":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out measureIterations))
                        {
                            error = $"Invalid --measure-iterations: {value}";
                            return false;
                        }

                        break;
                    case "--seed":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                        {
                            error = $"Invalid --seed: {value}";
                            return false;
                        }

                        break;
                    case "--target-fps":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out fps))
                        {
                            error = $"Invalid --target-fps: {value}";
                            return false;
                        }

                        break;
                    case "--output":
                        output = value;
                        break;
                    case "--startup-timeout-seconds":
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out startupTimeout))
                        {
                            error = $"Invalid --startup-timeout-seconds: {value}";
                            return false;
                        }

                        break;
                    case "--project-root":
                        projectRoot = value;
                        break;
                    case "--scene-file":
                        sceneFile = value;
                        break;
                    default:
                        error = $"Unknown perf-run argument: {arg}";
                        return false;
                }
            }
            else if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Missing value for {arg}";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(scene))
        {
            var parts = scene.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "--scene must be CATEGORY:TestId (e.g. 3D:InstancedMesh)";
                return false;
            }

            category = parts[0];
            testId = parts[1];
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            error = "--output is required for --perf-run";
            return false;
        }

        if (warmup < 0f || measure <= 0f)
        {
            error = "warmup must be >= 0 and measure must be > 0";
            return false;
        }

        if (measureIterations < 1)
        {
            error = "measure-iterations must be >= 1";
            return false;
        }

        options = new PerfRunOptions(
            category, testId, count, width, height, warmup, measure, measureIterations, seed, fps, output,
            startupTimeout, projectRoot, sceneFile);
        return true;
    }

    private static bool TryReadValue(string[] args, ref int i, out string value)
    {
        value = "";
        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            return false;
        value = args[++i];
        return true;
    }
}
