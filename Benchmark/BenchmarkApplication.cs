using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Ui.ImGui;

namespace Benchmark;

public class BenchmarkApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, rendererApi, graphics2D, graphics3D, audio, meshFactory, imGuiLayer, imGuiLayer);
