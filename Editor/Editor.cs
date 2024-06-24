using Engine.Core;

namespace Editor;

public class Editor : Application
{
    public Editor()
    {
        PushLayer(new EditorLayer("Editor Layer"));
    }
}