using Engine.Core;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.ImGuiNet;

namespace Editor;

public class Editor : Application
{
    public Editor(IGameWindow gameWindow, IImGuiLayer imGuiLayer) : base(gameWindow, imGuiLayer)
    {
    }
}