using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication(IGameWindow gameWindow, IGraphics2D graphics2D) : base(gameWindow, graphics2D)
    {
    }
}