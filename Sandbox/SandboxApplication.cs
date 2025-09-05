using Engine.Core;
using Engine.Core.Window;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication(IGameWindow gameWindow) : base(gameWindow, true)
    {
    }
}