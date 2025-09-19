using DryIoc;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Sandbox;

public class Program
{
    public static void Main(string[] args)
    {
        var props = new WindowProps("Sandbox Engine testing!", 1280, 720);

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

        // Register EditorLayer with constructor injection
        container.Register<Sandbox2DLayer>(Reuse.Singleton);
        container.Register<IImGuiLayer, ImGuiLayer>(Reuse.Singleton);
        
        container.ValidateAndThrow();

        try
        {
            var gameWindow = container.Resolve<IGameWindow>();
            var sandboxLayer = container.Resolve<Sandbox2DLayer>();
            var imGuiLayer = container.Resolve<IImGuiLayer>();
            var app = new SandboxApplication(gameWindow, imGuiLayer);
            app.PushLayer(sandboxLayer);
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }
}