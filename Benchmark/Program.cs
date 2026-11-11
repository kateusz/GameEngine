using Benchmark.Cli;
using DryIoc;
using Serilog;

namespace Benchmark;

public class Program
{
    public static int Main(string[] args)
    {
        if (PerfRunOptions.TryParse(args, out var cli, out var parseError))
            return RunCli(cli!);

        if (args.Contains(PerfRunOptions.CliFlag, StringComparer.Ordinal))
        {
            Console.Error.WriteLine(parseError ?? "Invalid --perf-run arguments");
            return 1;
        }

        RunInteractive();
        return 0;
    }

    private static int RunCli(PerfRunOptions cli)
    {
        Container? container = null;
        CliBenchmarkLayer? layer = null;
        try
        {
            container = BenchmarkHost.CreateCli(cli);
            container.ValidateAndThrow();
            layer = container.Resolve<CliBenchmarkLayer>();
            var app = container.Resolve<BenchmarkApplication>();
            app.PushLayer(layer);
            app.Run();
            return layer.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            try
            {
                PerfMetricsWriter.WriteJson(cli.OutputPath,
                    new BenchmarkResult { TestName = $"{cli.Category}:{cli.TestId}" },
                    cli, false, ex.Message, "", null);
            }
            catch
            {
                // ignored
            }

            return 2;
        }
        finally
        {
            Log.CloseAndFlush();
            container?.Dispose();
        }
    }

    private static void RunInteractive()
    {
        var container = BenchmarkHost.CreateInteractive();
        try
        {
            container.ValidateAndThrow();
            var app = container.Resolve<BenchmarkApplication>();
            app.PushLayer(container.Resolve<BenchmarkLayer>());
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
            container.Dispose();
        }
    }
}
