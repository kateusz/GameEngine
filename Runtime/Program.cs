using DryIoc;
using Engine.Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Serilog;
using Serilog.Events;
using System.Text.Json;

namespace Runtime;

public class Program
{
    private static readonly ILogger Logger = Log.Logger;

    public static void Main(string[] args)
    {
        ConfigureLogging();

        try
        {
            var runtimeConfig = LoadRuntimeConfig(args);
            Logger.Information("Starting game runtime...");
            Logger.Information("Startup scene: {Scene}", runtimeConfig.StartupScene);
            Logger.Information("Window: {Width}x{Height}, Fullscreen: {Fullscreen}",
                runtimeConfig.WindowWidth, runtimeConfig.WindowHeight, runtimeConfig.Fullscreen);

            var container = new Container();
            ConfigureContainer(container, runtimeConfig);

            var app = container.Resolve<RuntimeApplication>();
            var runtimeLayer = container.Resolve<RuntimeLayer>();
            app.PushLayer(runtimeLayer);
            app.Run();
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "Fatal error in game runtime");
            Console.WriteLine($"Fatal error: {e.Message}");
            Environment.Exit(1);
        }
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/runtime-.log", rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static RuntimeConfig LoadRuntimeConfig(string[] args)
    {
        var config = new RuntimeConfig();

        // Try to load from RuntimeConfig.json first
        var configPath = Path.Combine(AppContext.BaseDirectory, "RuntimeConfig.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var loadedConfig = JsonSerializer.Deserialize<RuntimeConfig>(json);
                if (loadedConfig != null)
                {
                    config = loadedConfig;
                    Logger.Information("Loaded configuration from RuntimeConfig.json");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load RuntimeConfig.json, using defaults");
            }
        }

        // Override with command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--scene":
                    if (i + 1 < args.Length)
                        config.StartupScene = args[++i];
                    break;
                case "--fullscreen":
                    config.Fullscreen = true;
                    break;
                case "--windowed":
                    config.Fullscreen = false;
                    break;
                case "--width":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var width))
                        config.WindowWidth = width;
                    break;
                case "--height":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var height))
                        config.WindowHeight = height;
                    break;
                case "--vsync":
                    config.VSync = true;
                    break;
                case "--no-vsync":
                    config.VSync = false;
                    break;
            }
        }

        // Validate startup scene
        if (string.IsNullOrEmpty(config.StartupScene))
        {
            throw new InvalidOperationException("No startup scene specified. Use --scene argument or RuntimeConfig.json");
        }

        return config;
    }

    private static void ConfigureContainer(Container container, RuntimeConfig config)
    {
        // Register runtime configuration
        container.RegisterInstance(config);

        // Window setup
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(config.WindowWidth, config.WindowHeight);
        options.Title = config.WindowTitle;
        options.VSync = config.VSync;
        options.WindowState = config.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;

        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Window.Create(options))
        );

        container.Register<IGameWindow>(Reuse.Singleton,
            made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
        );

        // Graphics and audio
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<IAudioEngine, AudioEngine>(Reuse.Singleton);

        // Scene systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<SceneSystemRegistry>(Reuse.Singleton);
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);
        container.Register<AudioSystem>(Reuse.Singleton);

        // Runtime layer and application
        container.Register<RuntimeLayer>(Reuse.Singleton);
        container.Register<RuntimeApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}

public class RuntimeConfig
{
    public string StartupScene { get; set; } = "";
    public string WindowTitle { get; set; } = "Game";
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
}
