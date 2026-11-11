using DryIoc;
using Benchmark.Cli;
using Engine.Core.DI;
using Engine.Project;
using Ui.ImGui;
using Ui.ImGui.DI;

namespace Benchmark;

public static class BenchmarkHost
{
    public static Container CreateInteractive()
    {
        var container = new Container();
        EngineIoCContainer.RegisterCore(container);
        container.Resolve<IProjectContext>().Apply(AppContext.BaseDirectory);
        EngineIoCContainer.RegisterWindowing(container, new EngineHostOptions("Benchmark", 1280, 720)
        {
            EnsureRuntimeMeshSiblingOnLoad = true
        });
        ImGuiIoCContainer.Register(container);
        container.Register<BenchmarkLayer>(Reuse.Singleton);
        container.Register<BenchmarkApplication>(Reuse.Singleton);
        return container;
    }

    public static Container CreateCli(PerfRunOptions cli)
    {
        var container = new Container();
        EngineIoCContainer.RegisterCore(container);
        var projectContext = container.Resolve<IProjectContext>();
        if (!string.IsNullOrWhiteSpace(cli.ProjectRoot))
        {
            if (!BenchmarkProjectAccess.TryOpenProject(cli.ProjectRoot, projectContext, out var err))
                throw new InvalidOperationException(err ?? "Failed to open project");
        }
        else
        {
            projectContext.Apply(AppContext.BaseDirectory);
        }

        EngineIoCContainer.RegisterWindowing(container, new EngineHostOptions("PerfRun", cli.Width, cli.Height)
        {
            EnsureRuntimeMeshSiblingOnLoad = true,
            TargetFrameRate = cli.TargetFrameRate
        });
        container.RegisterInstance(cli);
        container.RegisterInstance<IImGuiLayer>(new HeadlessImGuiLayer());
        container.Register<BenchmarkRunnerFactory>(Reuse.Singleton);
        container.Register<CliBenchmarkLayer>(Reuse.Singleton);
        container.Register<BenchmarkApplication>(Reuse.Singleton);
        return container;
    }
}
