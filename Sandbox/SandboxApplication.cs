using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Ui.ImGui;

namespace Sandbox;

public class SandboxApplication(
    IGameWindow gameWindow,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, graphics2D, graphics3D, audio, meshFactory, imGuiLayer, imGuiLayer);
