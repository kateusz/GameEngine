using Engine.Core;
using Engine.Core.Window;
using Sandbox.Benchmark;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow) : base(gameWindow, true)
    {
    }
}