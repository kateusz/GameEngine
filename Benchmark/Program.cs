using DryIoc;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        var props = new WindowProps("Benchmark Engine", (int)DisplayConfig.DefaultWindowWidth, (int)DisplayConfig.DefaultWindowHeight);

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

        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEngine, Engine.Platform.SilkNet.Audio.SilkNetAudioEngine>(Reuse.Singleton);

        container.Register<Engine.Events.EventBus, Engine.Events.EventBus>(Reuse.Singleton);
        container.Register<Engine.Animation.AnimationAssetManager>(Reuse.Singleton);

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
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
    }
}