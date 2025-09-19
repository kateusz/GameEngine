using DryIoc;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Sandbox.Benchmark;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Benchmark;

public class Program
{
    public static void Main(string[] args)
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
        container.Register<IImGuiLayer, ImGuiLayer>(Reuse.Singleton);
        
        container.ValidateAndThrow();

        try
        {
            var gameWindow = container.Resolve<IGameWindow>();
            var layer = container.Resolve<BenchmarkLayer>();
            var imGuiLayer = container.Resolve<IImGuiLayer>();
            var app = new BenchmarkApplication(gameWindow, imGuiLayer);
            app.PushLayer(layer);
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }
}