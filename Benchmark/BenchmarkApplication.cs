using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.ImGuiNet;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow, IImGuiLayer imGuiLayer) : base(gameWindow, imGuiLayer)
    {
    }
}