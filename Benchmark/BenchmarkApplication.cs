using Engine.Core;
using Sandbox.Benchmark;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication() : base(true)
    {
        PushLayer(new BenchmarkLayer("Benchmark Layer"));
    }
}