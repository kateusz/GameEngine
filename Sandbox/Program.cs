using DryIoc;
using Engine.Core;
using Engine.Core.DI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Sandbox;

public class Program
{
    public static int Main(string[] args)
    {
        var container = new Container();
        try
        {
            ConfigureContainer(container);
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Application", "Sandbox")
                .Enrich.WithThreadId()
                .WriteTo.Async(a =>
                    a.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        theme: ConsoleTheme.None))
                .CreateLogger();

            Log.Information("Sandbox has started.");

            var app = container.Resolve<SandboxApplication>();
            var sandboxLayer = container.Resolve<ILayer>();
            app.PushLayer(sandboxLayer);
            app.Run();
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal application error: {ex.GetType().Name}");
            Console.Error.WriteLine($"Message: {ex.Message}");
            Console.Error.WriteLine($"Stack trace:\n{ex.StackTrace}");
            
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                Console.Error.WriteLine($"\nInner Exception: {innerEx.GetType().Name}");
                Console.Error.WriteLine($"Message: {innerEx.Message}");
                Console.Error.WriteLine($"Stack trace:\n{innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
            }

            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
            container.Dispose();
        }
    }

    private static void ConfigureContainer(Container container)
    {
        EngineIoCContainer.Register(container);

        container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
        container.Register<Engine.Scene.ModelSceneImporter>(Reuse.Singleton);
        container.Register<ILayer, PingPongLayer>(Reuse.Singleton);
        container.Register<SandboxApplication>(Reuse.Singleton);

        container.ValidateAndThrow();
    }
}