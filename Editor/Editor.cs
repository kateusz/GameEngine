using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Input;
using Ui.ImGui;

namespace Editor;

public class Editor(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IImGuiLayer imGuiLayer,
    IKeyboardInput keyboardInput,
    IMouseInput mouseInput)
    : Application(gameWindow, rendererApi, graphics2D, graphics3D, audio, meshFactory, imGuiLayer, imGuiLayer, keyboardInput, mouseInput);
