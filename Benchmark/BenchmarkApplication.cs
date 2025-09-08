using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Sandbox.Benchmark;

namespace Benchmark;

public class BenchmarkApplication : Application
{
    public BenchmarkApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IGraphics3D graphics3D) : base(
        gameWindow, graphics2D, graphics3D, true)
    {
    }
}