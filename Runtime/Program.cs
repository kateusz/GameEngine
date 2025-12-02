using DryIoc;
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
        try
        {
            Logger.Information("Starting game runtime...");
            
            var container = new Container();
            ConfigureContainer(container);

            var app = container.Resolve<RuntimeApplication>();
            var gameLayer = container.Resolve<ILayer>();
            app.PushLayer(gameLayer);
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
        EngineIoCContainer.Register(container);
        
        container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
        container.Register<ILayer, GameLayer>(Reuse.Singleton);
        container.Register<RuntimeApplication>(Reuse.Singleton);
        
        container.ValidateAndThrow();
    }
}
