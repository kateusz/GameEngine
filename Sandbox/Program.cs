using DryIoc;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Sandbox;

public class Program
{
    public static void Main(string[] args)
    {
        var container = new Container();
        ConfigureContainer(container);

        try
        {
            var app = container.Resolve<SandboxApplication>();
            var sandboxLayer = container.Resolve<ILayer>();
            app.PushLayer(sandboxLayer);
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }

    private static void ConfigureContainer(Container container)
    {
        var props = new WindowProps("Editor", 1280, 720);
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";

        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Window.Create(options))
        );

        container.Register<IGameWindow>(Reuse.Singleton,
            made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
        );

        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        
        // Register SceneSystemRegistry and systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<SceneSystemRegistry>(Reuse.Singleton);
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);

        container.Register<ILayer, Sandbox2DLayer>(Reuse.Singleton);
        container.Register<SandboxApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}