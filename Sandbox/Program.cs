using DryIoc;
using Engine.Core;
using Engine.Core.DI;

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
        EngineIoCContainer.Register(container);
        
        container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
        container.Register<ILayer, Sandbox2DLayer>(Reuse.Singleton);
        container.Register<SandboxApplication>(Reuse.Singleton);
        
        container.ValidateAndThrow();
    }
}