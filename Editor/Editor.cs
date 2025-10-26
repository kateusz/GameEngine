using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Renderer;

namespace Editor;

public class Editor : Application
{
    public Editor(IGameWindow gameWindow, IGraphics2D graphics2D, IImGuiLayer imGuiLayer) : base(gameWindow, graphics2D, imGuiLayer)
    {
    }
}