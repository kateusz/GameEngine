using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IImGuiLayer imGuiLayer, IGraphics3D graphics3D)
        : base(gameWindow, graphics2D, graphics3D, imGuiLayer)
    {
    }
}