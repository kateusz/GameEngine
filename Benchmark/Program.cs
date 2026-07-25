using DryIoc;
using Engine.Core;
using Engine.Core.DI;
using Serilog;
using Ui.ImGui.DI;

namespace Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        var container = new Container();
        
        try
        {
            EngineIoCContainer.RegisterCore(container);
            // PathBuilder wires via RegisterInitializer on first IProjectContext resolve (same as Runtime).
            container.Resolve<IProjectContext>().Apply(AppContext.BaseDirectory);
            EngineIoCContainer.RegisterWindowing(container, new EngineHostOptions("Benchmark", 1280, 720));
            ImGuiIoCContainer.Register(container);
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