using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Ui.ImGui;

namespace Sandbox;

public class SandboxApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, rendererApi, graphics2D, graphics3D, audio, meshFactory, imGuiLayer, imGuiLayer);
