using Engine.Core;
using Engine.Core.Window;
using Engine.Platform.SilkNet;
using Silk.NET.Windowing;

namespace Sandbox;

public class SandboxApplication : Application
{
    public SandboxApplication(IGameWindow gameWindow) : base(gameWindow, true)
    {
    }
}