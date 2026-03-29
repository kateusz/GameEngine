using DryIoc;
using ECS;
using Engine.Core.DI;
using Serilog;

namespace Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        var container = new Container();
        
        try
        {
            EngineIoCContainer.Register(container);
            container.Register<IContext, Context>(Reuse.Singleton);
            container.Register<BenchmarkLayer>(Reuse.Singleton);
            container.Register<BenchmarkApplication>(Reuse.Singleton);
            container.ValidateAndThrow();

            var layer = container.Resolve<BenchmarkLayer>();
            var app = container.Resolve<BenchmarkApplication>();
            app.PushLayer(layer);
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
            container.Dispose();
        }
    }
}