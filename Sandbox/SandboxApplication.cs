using Engine.Core;
using Engine.Core.Window;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication(IGameWindow gameWindow, IGraphics2D graphics2D, IGraphics3D graphics3D) : base(gameWindow,
        graphics2D, graphics3D,
        true)
    {
    }
}