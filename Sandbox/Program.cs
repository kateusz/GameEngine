using DryIoc;
using Engine.Animation;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events;
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
        try
        {
            var container = new Container();
            ConfigureContainer(container);

            var app = container.Resolve<SandboxApplication>();
            var sandboxLayer = container.Resolve<ILayer>();
            app.PushLayer(sandboxLayer);
            app.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal application error: {ex.GetType().Name}");
            Console.Error.WriteLine($"Message: {ex.Message}");
            Console.Error.WriteLine($"Stack trace:\n{ex.StackTrace}");

            // Log inner exceptions if present
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                Console.Error.WriteLine($"\nInner Exception: {innerEx.GetType().Name}");
                Console.Error.WriteLine($"Message: {innerEx.Message}");
                Console.Error.WriteLine($"Stack trace:\n{innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
            }

            Environment.Exit(1);
        }
    }

    private static void ConfigureContainer(Container container)
    {
        var props = new WindowProps("Sandbox", (int)DisplayConfig.DefaultWindowWidth, (int)DisplayConfig.DefaultWindowHeight);
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";

        container.Register<IRendererApiConfig>(Reuse.Singleton,
            made: Made.Of(() => new RendererApiConfig(ApiType.SilkNet))
        );
        container.Register<IRendererAPI>(
            made: Made.Of(
                r => ServiceInfo.Of<IRendererApiFactory>(),
                f => f.Create()
            )
        );
        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Window.Create(options))
        );
        container.Register<IGameWindowFactory, GameWindowFactory>(Reuse.Singleton);
        container.Register<IGameWindow>(
            made: Made.Of(
                r => ServiceInfo.Of<IGameWindowFactory>(),
                f => f.Create()
            )
        );

        container.Register<EventBus, EventBus>(Reuse.Singleton);
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEngine, Engine.Platform.SilkNet.Audio.SilkNetAudioEngine>(Reuse.Singleton);
        
        // Register SceneSystemRegistry and systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<SceneSystemRegistry>(Reuse.Singleton);
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);
        container.Register<AudioSystem>(Reuse.Singleton);
        container.Register<AnimationSystem>(Reuse.Singleton);
        container.Register<AnimationAssetManager>(Reuse.Singleton);

        container.Register<ILayer, Sandbox2DLayer>(Reuse.Singleton);
        container.Register<SandboxApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}