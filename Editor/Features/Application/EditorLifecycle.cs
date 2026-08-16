using ECS;
using Editor.Features.History;
using Editor.Features.Scripting;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Input;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Scene;
using SceneComponents;
using Serilog;

namespace Editor.Features.Application;

public class EditorLifecycle(
    IProjectManager projectManager,
    IProjectContext projectContext,
    IEditorPreferences editorPreferences,
    DebugSettings debugSettings,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    GameScriptWorkspace scriptWorkspace,
    ShortcutManager shortcutManager,
    EditorShortcutRegistrar shortcutRegistrar,
    IEditorSelection selection,
    IEditorHistory history,
    IEditorViewport editorViewport,
    ISceneHierarchyPanel sceneHierarchyPanel,
    IContentBrowserPanel contentBrowserPanel,
    IConsolePanel consolePanel,
    ViewportComponents viewport)
{
    private static readonly ILogger Logger = Log.ForContext<EditorLifecycle>();

    private Action<IScene> _sceneChangedHandler = null!;
    private Action _playSceneHandler = null!;
    private Action _stopSceneHandler = null!;
    private Action _restartSceneHandler = null!;
    private Action<Entity?, SelectionSource> _selectionChangedHandler = null!;
    private Action _projectOpenedHandler = null!;
    private Action _projectClosingHandler = null!;
    private Action _projectClosedHandler = null!;

    public void Attach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLifecycle Attach.");

        _projectClosingHandler = () =>
        {
            if (sceneContext.State == SceneState.Play)
                sceneManager.Stop();
            else
                sceneContext.ActiveScene?.Dispose();

            scriptWorkspace.RevokeAndUnload();
        };

        _projectOpenedHandler = () =>
            contentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);

        _projectClosedHandler = () =>
        {
            contentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);
            sceneManager.New("");
        };

        projectManager.ProjectClosing += _projectClosingHandler;
        projectManager.ProjectOpened += _projectOpenedHandler;
        projectManager.ProjectClosed += _projectClosedHandler;

        _sceneChangedHandler = newScene =>
        {
            sceneHierarchyPanel.SetScene(newScene);
            viewport.SceneToolbar.ApplyGridFromScene(newScene);
            history.Clear();
        };
        _playSceneHandler = sceneManager.Play;
        _stopSceneHandler = sceneManager.Stop;
        _restartSceneHandler = sceneManager.Restart;
        _selectionChangedHandler = OnSelectionChanged;

        sceneContext.SceneChanged += _sceneChangedHandler;
        selection.SelectionChanged += _selectionChangedHandler;
        viewport.SceneToolbar.OnPlayScene += _playSceneHandler;
        viewport.SceneToolbar.OnStopScene += _stopSceneHandler;
        viewport.SceneToolbar.OnRestartScene += _restartSceneHandler;

        editorViewport.Initialize();

        sceneManager.New("");

        contentBrowserPanel.Init();
        viewport.SceneToolbar.Init();

        debugSettings.ShowColliderBounds = editorPreferences.ShowColliderBounds;
        debugSettings.ShowFPS = editorPreferences.ShowFPS;

        shortcutRegistrar.RegisterAll(shortcutManager);

        Logger.Information("Editor initialized successfully!");
        Logger.Information("Console panel is now capturing output.");
    }

    public void Detach()
    {
        Logger.Debug("EditorLifecycle Detach.");

        projectManager.ProjectClosing -= _projectClosingHandler;
        projectManager.ProjectOpened -= _projectOpenedHandler;
        projectManager.ProjectClosed -= _projectClosedHandler;
        sceneContext.SceneChanged -= _sceneChangedHandler;
        selection.SelectionChanged -= _selectionChangedHandler;
        viewport.SceneToolbar.OnPlayScene -= _playSceneHandler;
        viewport.SceneToolbar.OnStopScene -= _stopSceneHandler;
        viewport.SceneToolbar.OnRestartScene -= _restartSceneHandler;

        sceneContext.ActiveScene?.Dispose();
        editorViewport.Dispose();
        consolePanel?.Dispose();
    }

    private void OnSelectionChanged(Entity? entity, SelectionSource source)
    {
        if (source != SelectionSource.Hierarchy || entity is null)
            return;

        if (entity.TryGetComponent<TransformComponent>(out var transformComponent))
            editorViewport.Camera.SetFocalPoint(transformComponent.Translation);
    }
}
