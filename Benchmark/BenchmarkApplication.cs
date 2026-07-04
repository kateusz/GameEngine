using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Benchmark;

public class BenchmarkApplication(
    IGameWindow gameWindow,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, graphics2D, graphics3D, audio, meshFactory, imGuiLayer);