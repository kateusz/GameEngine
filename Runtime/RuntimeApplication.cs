using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Input;

namespace Runtime;

public class RuntimeApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IAudio audio,
    IKeyboardInput keyboardInput,
    IMouseInput mouseInput)
    : Application(gameWindow, rendererApi, graphics2D, audio, keyboardInput: keyboardInput, mouseInput: mouseInput);
