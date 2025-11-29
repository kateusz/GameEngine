using Engine.Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Editor;

public class Editor(
    IGameWindow gameWindow,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudioEngine audioEngine,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer)
    : Application(gameWindow, graphics2D, graphics3D, audioEngine, meshFactory, imGuiLayer);