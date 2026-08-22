using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Ui.ImGui;

namespace Sandbox;

public class SandboxApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IAudio audio,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, rendererApi, graphics2D, audio, imGuiLayer, imGuiLayer);
