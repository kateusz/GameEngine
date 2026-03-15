using System.Text.Json;
using DryIoc;
using ECS;
using Engine.Core;
using Engine.Core.DI;
using Serilog;

namespace Runtime;

/// <summary>
/// Entry point for the standalone game runtime.
/// This project is built and published by the GamePublisher to create standalone game executables.
/// </summary>
public class Program
{
    private static readonly ILogger Logger = Log.ForContext<Program>();
    
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("Application", "GameEngine")
            .Enrich.WithThreadId()
            .WriteTo.Async(a => a.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Async(a => a.File("logs/runtime-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();
        
        Logger.Information("Starting game runtime...");
        
        try
        {
            var gameConfig = LoadGameConfiguration();
            Logger.Information("Game: {Title}", gameConfig.GameTitle);
            Logger.Information("Startup Scene: {Scene}", gameConfig.StartupScenePath);

            var container = new Container();
            ConfigureContainer(container, gameConfig);

            var app = container.Resolve<RuntimeApplication>();
            var gameLayer = container.Resolve<ILayer>();
            app.PushLayer(gameLayer);
            app.Run();
            container.Dispose();
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

    private static GameConfiguration LoadGameConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "game.config.json");

        if (!File.Exists(configPath))
        {
            Logger.Warning("Game configuration not found at {Path}, using defaults", configPath);
            return new GameConfiguration();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<GameConfiguration>(json);

            if (config == null)
            {
                Logger.Warning("Failed to deserialize game configuration, using defaults");
                return new GameConfiguration();
            }

            return config;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load game configuration, using defaults");
            return new GameConfiguration();
        }
    }

    private static void ConfigureContainer(Container container, GameConfiguration gameConfig)
    {
        EngineIoCContainer.Register(container);
        container.Register<IContext, Context>(Reuse.Singleton);
        container.RegisterInstance(gameConfig);
        container.Register<ILayer, GameLayer>(Reuse.Singleton);
        container.Register<RuntimeApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}
