using DryIoc;
using Engine;
using Engine.Animation;
using Engine.Core;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using Silk.NET.Maths;
using Silk.NET.Windowing;

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
    }
}