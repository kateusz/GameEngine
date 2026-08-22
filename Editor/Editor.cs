using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Input;
using Ui.ImGui;

namespace Editor;

public class Editor(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IAudio audio,
    IImGuiLayer imGuiLayer,
    IKeyboardInput keyboardInput,
    IMouseInput mouseInput)
    : Application(gameWindow, rendererApi, graphics2D, audio, imGuiLayer, imGuiLayer, keyboardInput, mouseInput);
