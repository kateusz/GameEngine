using Engine.Core;

namespace Editor;

public class Editor : Application
{
    public Editor() : base(true)
    {
        PushLayer(new EditorLayer("Editor Layer"));
    }
}