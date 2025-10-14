using DryIoc;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Benchmark;

public class Program
{
    public static int Main(string[] args)
    {
        // Parse command line arguments
        var config = BenchmarkConfig.ParseArgs(args);

        // Run in headless mode if specified
        if (config.Headless)
        {
            return RunHeadless(config);
        }

        // Otherwise run with GUI
        return RunWithGUI();
    }

    private static int RunHeadless(BenchmarkConfig config)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        Game Engine Benchmark - Headless Mode              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var runner = new HeadlessBenchmarkRunner(config);
            return runner.Run();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFatal error: {ex.Message}");
            Console.ResetColor();

            if (config.Verbose)
            {
                Console.WriteLine("\nStack trace:");
                Console.WriteLine(ex.StackTrace);
            }

            return 2; // Error exit code
        }
    }

    private static int RunWithGUI()
    {
        var props = new WindowProps("Benchmark Engine", 1280, 720);

        var container = new Container();

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";

        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Window.Create(options))
        );

        container.Register<IGameWindow>(Reuse.Singleton,
            made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
        );

        container.Register<BenchmarkLayer>(Reuse.Singleton);
        container.Register<BenchmarkApplication>(Reuse.Singleton);
        container.Register<IImGuiLayer, ImGuiLayer>(Reuse.Singleton);

        container.ValidateAndThrow();

        try
        {
            var layer = container.Resolve<BenchmarkLayer>();
            var app = container.Resolve<BenchmarkApplication>();
            app.PushLayer(layer);
            app.Run();
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Application error: {e.Message}");
            return 2;
        }
    }
}