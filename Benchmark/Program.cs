using Benchmark;
using DryIoc;
using Editor.Components;
using Editor.State;
using Engine.Core.Window;
using Sandbox.Benchmark;
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

        // Register EditorLayer dependencies
        container.Register<EditorState>(Reuse.Singleton);
        container.Register<IEditorViewport, EditorViewport>(Reuse.Singleton);
        container.Register<IEditorUIRenderer, EditorUIRenderer>(Reuse.Singleton);
        container.Register<IEditorPerformanceMonitor, EditorPerformanceMonitor>(Reuse.Singleton);
        container.Register<Workspace>(Reuse.Singleton);
        container.Register<ProjectController>(Reuse.Singleton);
        container.Register<SceneController>(Reuse.Singleton);
        container.Register<EditorInputHandler>(Reuse.Singleton);

        // Register EditorLayer with constructor injection
        container.Register<BenchmarkLayer>(Reuse.Singleton);

        try
        {
            var gameWindow = container.Resolve<IGameWindow>();
            var layer = container.Resolve<BenchmarkLayer>();
            var app = new BenchmarkApplication(gameWindow);
            app.PushLayer(layer);
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }
}