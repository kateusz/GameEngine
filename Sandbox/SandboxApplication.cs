using Engine.Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IGraphics3D graphics3D, IAudioEngine audioEngine)
        : base(gameWindow, graphics2D, graphics3D, audioEngine)
    {
    }
}