using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IImGuiLayer imGuiLayer) : base(gameWindow, graphics2D, imGuiLayer)
    {
    }
}