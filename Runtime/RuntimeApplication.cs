using Engine.Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;

namespace Runtime;

/// <summary>
/// The main application class for the standalone game runtime.
/// </summary>
public class RuntimeApplication : Application
{
    public RuntimeApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IGraphics3D graphics3D,
        IAudioEngine audioEngine, IMeshFactory meshFactory)
        : base(gameWindow, graphics2D, graphics3D, audioEngine, meshFactory)
    {
    }
}
