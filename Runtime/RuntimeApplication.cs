using Engine.Audio;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer;

namespace Runtime;

/// <summary>
/// Minimal application for running published games.
/// No ImGui, no editor features - just the game.
/// </summary>
public class RuntimeApplication : Application
{
    public RuntimeApplication(
        IGameWindow gameWindow,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        IAudioEngine audioEngine)
        : base(gameWindow, graphics2D, graphics3D, audioEngine, imGuiLayer: null)
    {
        // No ImGui layer for runtime builds
    }
}
