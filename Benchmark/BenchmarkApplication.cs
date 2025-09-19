using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Sandbox.Benchmark;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow, IImGuiLayer imGuiLayer) : base(gameWindow, imGuiLayer)
    {
    }
}