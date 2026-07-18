using Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;
using Input;

namespace Runtime;

/// <summary>
/// The main application class for the standalone game runtime.
/// </summary>
public class RuntimeApplication(
    IGameWindow gameWindow,
    IRendererAPI rendererApi,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IAudio audio,
    IMeshFactory meshFactory,
    IKeyboardInput keyboardInput)
    : Application(gameWindow, rendererApi, graphics2D, graphics3D, audio, meshFactory, keyboardInput: keyboardInput)
{
}
