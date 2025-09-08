using DryIoc;
using Editor.Managers;
using Editor.Panels;
using Engine.Core;

namespace Editor;

public class EditorModule : IModule
{
    public int Priority => 100; // Application-specific

    public void Register(IContainer container)
    {
        container.Register<EditorLayer>(Reuse.Singleton);
        container.Register<SceneManager>(Reuse.Singleton);
        container.Register<ContentBrowserPanel>(Reuse.Singleton);
        container.Register<ConsolePanel>(Reuse.Singleton);
        container.Register<PropertiesPanel>(Reuse.Singleton);
        container.Register<ProjectManager>(Reuse.Singleton);
        container.Register<ProjectUI>(Reuse.Singleton);
        container.Register<EditorToolbar>(Reuse.Singleton);
        container.Register<EditorSettingsUI>(Reuse.Singleton);
        container.Register<EditorSettings>(Reuse.Singleton);
    }
}