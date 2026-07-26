using System.Reflection;
using DryIoc;
using ECS.Systems;
using Editor.ComponentEditors;
using Editor.ComponentEditors.Audio;
using Editor.ComponentEditors.Core;
using Editor.ComponentEditors.Physics;
using Editor.ComponentEditors.Rendering;
using Editor.Features.Components;
using Editor.Features.History;
using Editor.Features.Import;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Input;
using Editor.Panels;
using Editor.Publisher;
using Editor.UI.Elements;
using Editor.UI.FieldEditors;
using Editor.Features.Scripting;
using Editor.Features.Shell;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Engine.Core;
using Engine.Scene;
using Engine.Scripting;
using GameComponentEditor = Editor.ComponentEditors.GameComponentEditor;

namespace Editor.DI;

public static class EditorIoCContainer
{
    public static void Register(Container container)
    {
        container.Register<ShortcutManager>(Reuse.Singleton);
        container.Register<IEditorSelection, EditorSelection>(Reuse.Singleton);
        container.Register<IEditorHistory, EditorHistory>(Reuse.Singleton);
        
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

        container.RegisterMany<IntFieldEditor>(Reuse.Singleton);
        container.RegisterMany<FloatFieldEditor>(Reuse.Singleton);
        container.RegisterMany<DoubleFieldEditor>(Reuse.Singleton);
        container.RegisterMany<BoolFieldEditor>(Reuse.Singleton);
        container.RegisterMany<StringFieldEditor>(Reuse.Singleton);
        container.RegisterMany<Vector2FieldEditor>(Reuse.Singleton);
        container.RegisterMany<Vector3FieldEditor>(Reuse.Singleton);
        container.RegisterMany<Vector4FieldEditor>(Reuse.Singleton);
        container.Register<UIPropertyRenderer>(Reuse.Singleton);
    
        // Component editors — registration order = properties panel draw order
        container.RegisterMany<TransformComponentEditor>(Reuse.Singleton);
        container.RegisterMany<CameraComponentEditor>(Reuse.Singleton);
        container.RegisterMany<SpriteRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<ModelRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<SkeletalPlaybackComponentEditor>(Reuse.Singleton);
        container.RegisterMany<RigidBody2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<BoxCollider2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<CircleCollider2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<EdgeCollider2DComponentEditor>(Reuse.Singleton);
        container.RegisterMany<SubTextureRendererComponentEditor>(Reuse.Singleton);
        container.RegisterMany<AudioSourceComponentEditor>(Reuse.Singleton);
        container.RegisterMany<AudioListenerComponentEditor>(Reuse.Singleton);
        container.RegisterMany<GameComponentEditor>(Reuse.Singleton);
        container.RegisterMany<ScriptComponentEditor>(Reuse.Singleton);
        container.RegisterMany<AmbientLightComponentEditor>(Reuse.Singleton);
        container.RegisterMany<DirectionalLightComponentEditor>(Reuse.Singleton);
        container.Register<IComponentEditorRegistry, ComponentEditorRegistry>(Reuse.Singleton);

        // Panel draw order
        container.RegisterMany<SceneHierarchyPanel>(Reuse.Singleton);
        container.RegisterMany<PropertiesPanel>(Reuse.Singleton);
        container.RegisterMany<ContentBrowserPanel>(Reuse.Singleton);
        container.Register<ContentBrowserActions>(Reuse.Singleton);
        container.RegisterMany<ConsolePanel>(Reuse.Singleton);
        container.RegisterMany<RecentProjectsPanel>(Reuse.Singleton);
        container.RegisterMany<KeyboardShortcutsPanel>(Reuse.Singleton);

        container.Register<IEntityContextMenu, EntityContextMenu>(Reuse.Singleton);
        
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

        container.Register<PrefabDropTarget>(Reuse.Singleton);
        container.Register<NewProjectPopup>(Reuse.Singleton);
        container.Register<Import3DModelPopup>(Reuse.Singleton);
        container.Register<SceneSettingsPopup>(Reuse.Singleton);
        container.Register<SceneToolbar>(Reuse.Singleton);
        container.Register<RendererStatsPanel>(Reuse.Singleton);
        container.Register<Features.Components.GameComponentEditor>(Reuse.Singleton);
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

        container.Register<EditorPanels>(Reuse.Singleton);
        container.Register<ViewportComponents>(Reuse.Singleton);
        container.Register<ILayer, EditorLayer>(Reuse.Singleton);
        container.Register<Editor>(Reuse.Singleton);
    }

    private static bool RegisterGameAssembly(Container container, Assembly assembly) =>
        GameAssemblyContainerRegistration.TryRegisterContainer(container, assembly);
}