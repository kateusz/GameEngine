using Engine.Core;
using Sandbox.Benchmark;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication() : base(true)
    {
        //PushLayer(new Sandbox2DLayer("Sandbox 2D Layer"));
        PushLayer(new BenchmarkLayer("Benchmark Layer"));
    }
}