using DryIoc;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.UI.Elements;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Engine.Core;
using Engine.Scene.Systems;

namespace Editor.DI;

public static class EditorIoCContainer
{
    public static void Register(Container container)
    {
        container.Register<ShortcutManager>(Reuse.Singleton);
        
        container.Register<IProjectManager, ProjectManager>(Reuse.Singleton);
        container.Register<IEditorPreferences, EditorPreferences>(Reuse.Singleton,
            made: Made.Of(() => EditorPreferences.Load())
        );
        container.Register<EditorSettingsUI>(Reuse.Singleton);
        container.Register<AudioDropTarget>(Reuse.Singleton);
        container.Register<PerformanceMonitorPanel>(Reuse.Singleton);
    
        container.Register<TransformComponentEditor>(Reuse.Singleton);
        container.Register<CameraComponentEditor>(Reuse.Singleton);
        container.Register<SpriteRendererComponentEditor>(Reuse.Singleton);
        container.Register<MeshComponentEditor>(Reuse.Singleton);
        container.Register<ModelRendererComponentEditor>(Reuse.Singleton);
        container.Register<RigidBody2DComponentEditor>(Reuse.Singleton);
        container.Register<BoxCollider2DComponentEditor>(Reuse.Singleton);
        container.Register<SubTextureRendererComponentEditor>(Reuse.Singleton);
        container.Register<AudioSourceComponentEditor>(Reuse.Singleton);
        container.Register<AudioListenerComponentEditor>(Reuse.Singleton);
        container.Register<AnimationComponentEditor>(Reuse.Singleton);
        container.Register<AnimationTimelinePanel>(Reuse.Singleton);
        container.Register<RecentProjectsPanel>(Reuse.Singleton);
        container.Register<TileMapEditingSystem>(Reuse.Singleton);
        container.Register<TileMapPanel>(Reuse.Singleton);
        container.Register<TileMapComponentEditor>(Reuse.Singleton);
    
        container.Register<IComponentEditorRegistry, ComponentEditorRegistry>(Reuse.Singleton);
        container.Register<IPropertiesPanel, PropertiesPanel>(Reuse.Singleton);
        container.Register<ISceneHierarchyPanel, SceneHierarchyPanel>(Reuse.Singleton);
        container.Register<EntityContextMenu>(Reuse.Singleton);
        container.Register<PrefabDropTarget>(Reuse.Singleton);
        
        container.RegisterMany<SceneManager>(Reuse.Singleton);
        
        container.Register<IContentBrowserPanel, ContentBrowserPanel>(Reuse.Singleton);
        container.Register<NewProjectPopup>(Reuse.Singleton);
        container.Register<SceneSettingsPopup>(Reuse.Singleton);
        container.Register<SceneToolbar>(Reuse.Singleton);
        container.Register<RendererStatsPanel>(Reuse.Singleton);
        container.Register<KeyboardShortcutsPanel>(Reuse.Singleton);
        container.Register<ScriptComponentEditor>(Reuse.Singleton);
    
        // Viewport infrastructure
        container.Register<ViewportRuler>(Reuse.Singleton);

        // Viewport tools
        container.Register<SelectionTool>(Reuse.Singleton);
        container.Register<MoveTool>(Reuse.Singleton);
        container.Register<ScaleTool>(Reuse.Singleton);
        container.Register<RulerTool>(Reuse.Singleton);
        container.Register<ViewportToolManager>(Reuse.Singleton);
        
        container.Register<IPrefabManager, PrefabManager>(Reuse.Singleton);
        
        container.Register<IConsolePanel, ConsolePanel>(Reuse.Singleton);
        
        container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
        container.Register<ILayer, EditorLayer>(Reuse.Singleton);
        container.Register<Editor>(Reuse.Singleton);
    }
}