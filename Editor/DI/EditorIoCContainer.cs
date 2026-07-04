using System.Reflection;
using DryIoc;
using ECS.Systems;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Core;
using Editor.Features.Components;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.Publisher;
using Editor.UI.Elements;
using Editor.Features.Scripting;
using Editor.Features.Shell;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Engine.Core;
using Engine.Scene;
using Engine.Scripting;

namespace Editor.DI;

public static class EditorIoCContainer
{
    public static void Register(Container container)
    {
        container.Register<ShortcutManager>(Reuse.Singleton);
        container.Register<IEditorSelection, EditorSelection>(Reuse.Singleton);
        
        container.Register<IProjectManager, ProjectManager>(Reuse.Singleton);
        container.Register<GameScriptWorkspace>(Reuse.Singleton);
        container.Register<IGameProjectScriptBootstrapper, GameProjectScriptBootstrapper>(Reuse.Singleton);
        container.Register<IGamePublisher, GamePublisher>(Reuse.Singleton);
        container.Register<PublishSettingsUI>(Reuse.Singleton);
        container.Register<IEditorPreferences, EditorPreferences>(Reuse.Singleton,
            made: Made.Of(() => EditorPreferences.Load())
        );
        container.Register<EditorSettingsUI>(Reuse.Singleton);
        container.Register<AudioDropTarget>(Reuse.Singleton);
        container.Register<PerformanceMonitorPanel>(Reuse.Singleton);
    
        // Component editors — registration order = properties panel draw order
        container.RegisterMany<TransformComponentEditor>(Reuse.Singleton);
        container.RegisterMany<CameraComponentEditor>(Reuse.Singleton);
        container.RegisterMany<SpriteRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<ModelRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<RigidBody2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<BoxCollider2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<SubTextureRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<AudioSourceComponentEditor>(Reuse.Singleton);
        container.RegisterMany<AudioListenerComponentEditor>(Reuse.Singleton);
        container.RegisterMany<GameComponentInspector>(Reuse.Singleton);
        container.RegisterMany<ScriptComponentEditor>(Reuse.Singleton);
        container.Register<IComponentEditorRegistry, ComponentEditorRegistry>(Reuse.Singleton);
        container.Register<RecentProjectsPanel>(Reuse.Singleton);
    
        container.Register<IPropertiesPanel, PropertiesPanel>(Reuse.Singleton);
        container.Register<IEntityContextMenu, EntityContextMenu>(Reuse.Singleton);
        container.Register<ISceneHierarchyPanel, SceneHierarchyPanel>(Reuse.Singleton);
        container.Register<PrefabDropTarget>(Reuse.Singleton);
        
        container.RegisterDelegate<Func<Assembly, bool>>(
            _ => assembly => RegisterGameAssembly(container, assembly),
            Reuse.Singleton);

        container.RegisterDelegate<Action<Assembly>>(
            _ => assembly => GameAssemblyContainerRegistration.UnregisterRegistrationsFromGameAssembly(container, assembly),
            Reuse.Singleton);

        container.RegisterDelegate<Func<IEnumerable<IGameSystem>>>(
            r => () => r.ResolveMany<IGameSystem>(),
            Reuse.Singleton);

        container.RegisterMany<SceneManager>(Reuse.Singleton);
        
        container.Register<IContentBrowserPanel, ContentBrowserPanel>(Reuse.Singleton);
        container.Register<NewProjectPopup>(Reuse.Singleton);
        container.Register<SceneSettingsPopup>(Reuse.Singleton);
        container.Register<SceneToolbar>(Reuse.Singleton);
        container.Register<RendererStatsPanel>(Reuse.Singleton);
        container.Register<KeyboardShortcutsPanel>(Reuse.Singleton);
        container.Register<GameComponentEditor>(Reuse.Singleton);
        container.Register<IGameComponentFactory, GameComponentFactory>(Reuse.Singleton);
    
        // Viewport infrastructure
        container.Register<IViewportScaleHelper, ViewportScaleHelper>(Reuse.Singleton);
        container.Register<ViewportRuler>(Reuse.Singleton);
        container.Register<ViewportGrid>(Reuse.Singleton);
        container.Register<ViewportGrid3D>(Reuse.Singleton);

        // Viewport tools
        container.Register<SelectionTool>(Reuse.Singleton);
        container.Register<MoveTool>(Reuse.Singleton);
        container.Register<ScaleTool>(Reuse.Singleton);
        container.Register<RotateTool>(Reuse.Singleton);
        container.Register<RulerTool>(Reuse.Singleton);
        container.Register<ViewportToolManager>(Reuse.Singleton);

        container.Register<IEditorCameraController, EditorCameraController>(Reuse.Singleton);
        container.Register<IEditorViewport, EditorViewport>(Reuse.Singleton);
        container.Register<EditorMenuBar>(Reuse.Singleton);
        container.Register<EditorDockspace>(Reuse.Singleton);
        container.Register<EditorInputHandler>(Reuse.Singleton);
        container.Register<EditorShortcutRegistrar>(Reuse.Singleton);
        container.Register<EditorLifecycle>(Reuse.Singleton);
        
        container.Register<IPrefabManager, PrefabManager>(Reuse.Singleton);

        container.Register<IConsolePanel, ConsolePanel>(Reuse.Singleton);
        
        container.Register<EditorPanels>(Reuse.Singleton);
        container.Register<ViewportComponents>(Reuse.Singleton);
        container.Register<ILayer, EditorLayer>(Reuse.Singleton);
        container.Register<Editor>(Reuse.Singleton);
    }

    private static bool RegisterGameAssembly(Container container, Assembly assembly) =>
        GameAssemblyContainerRegistration.TryRegisterContainer(container, assembly);
}