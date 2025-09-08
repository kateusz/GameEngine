using DryIoc;
using Engine.Core;

namespace Editor;

public class EditorModule : IModule
{
    public int Priority => 100; // Application-specific

    public void Register(IContainer container)
    {
        container.Register<EditorLayer>(Reuse.Singleton);
    }
}