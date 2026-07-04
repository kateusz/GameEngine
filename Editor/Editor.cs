using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Input;
using Ui.ImGui;

namespace Editor;

public class Editor(
    IGameWindow gameWindow,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer,
    IKeyboardInput keyboardInput)
    : Application(gameWindow, graphics2D, graphics3D, audio, meshFactory, imGuiLayer, imGuiLayer, keyboardInput);
