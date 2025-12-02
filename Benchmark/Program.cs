using DryIoc;
using Engine.Core.DI;

namespace Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        var container = new Container();

        EngineIoCContainer.Register(container);
        container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
        container.Register<BenchmarkLayer>(Reuse.Singleton);
        container.Register<BenchmarkApplication>(Reuse.Singleton);
        container.ValidateAndThrow();

        var layer = container.Resolve<BenchmarkLayer>();
        var app = container.Resolve<BenchmarkApplication>();
        app.PushLayer(layer);
        app.Run();
        
        container.Dispose();
    }
}