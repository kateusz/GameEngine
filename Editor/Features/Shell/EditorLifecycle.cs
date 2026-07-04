using ECS;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Scripting;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Input;
using Engine.Core;
using Engine.Core.Input;
using Engine.Scene;
using Engine.Scripting;
using SceneComponents;
using Serilog;

namespace Editor.Features.Shell;

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
    IEditorViewport editorViewport,
    EditorPanels panels,
    ViewportComponents viewport)
{
    private static readonly ILogger Logger = Log.ForContext<EditorLifecycle>();

    private Action<IScene> _sceneChangedHandler = null!;
    private Action _playSceneHandler = null!;
    private Action _stopSceneHandler = null!;
    private Action _restartSceneHandler = null!;
    private Action<Entity?, SelectionSource> _selectionChangedHandler = null!;
    private Action<ProjectPaths> _projectOpenedHandler = null!;
    private Action _projectClosedHandler = null!;

    public void Attach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLifecycle Attach.");

        _projectOpenedHandler = paths =>
            panels.ContentBrowserPanel.SetRootDirectory(paths.AssetsDir);

        _projectClosedHandler = () =>
        {
            panels.ContentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);
            sceneManager.New("");
        };

        projectManager.ProjectOpened += _projectOpenedHandler;
        projectManager.ProjectClosed += _projectClosedHandler;

        _sceneChangedHandler = newScene =>
        {
            panels.SceneHierarchyPanel.SetScene(newScene);

            if (string.IsNullOrWhiteSpace(newScene.Name))
                return;

            if (projectManager.CurrentProjectDirectory is { } projectDir && projectManager.ScriptsDir is { } scriptsDir)
                scriptWorkspace.SetScriptsDirectory(scriptsDir, GameScriptWorkspace.ResolveEditorDllPath(projectDir));

#if DEBUG
            scriptWorkspace.EnableHybridDebugging(true);

            var symbolsPath = Path.Combine(Environment.CurrentDirectory, "DebugSymbols", "Scripts");
            Directory.CreateDirectory(symbolsPath);
            scriptWorkspace.SaveDebugSymbols(Path.Combine(symbolsPath, "GameAssembly"), "GameAssembly");
            scriptWorkspace.PrintDebugInfo();
#endif
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

        panels.ContentBrowserPanel.Init();
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

        projectManager.ProjectOpened -= _projectOpenedHandler;
        projectManager.ProjectClosed -= _projectClosedHandler;
        sceneContext.SceneChanged -= _sceneChangedHandler;
        selection.SelectionChanged -= _selectionChangedHandler;
        viewport.SceneToolbar.OnPlayScene -= _playSceneHandler;
        viewport.SceneToolbar.OnStopScene -= _stopSceneHandler;
        viewport.SceneToolbar.OnRestartScene -= _restartSceneHandler;

        sceneContext.ActiveScene?.Dispose();
        editorViewport.Dispose();
        panels.ConsolePanel?.Dispose();
    }

    private void OnSelectionChanged(Entity? entity, SelectionSource source)
    {
        if (source != SelectionSource.Hierarchy || entity is null)
            return;

        if (entity.TryGetComponent<TransformComponent>(out var transformComponent))
            editorViewport.Camera.SetFocalPoint(transformComponent.Translation);
    }
}
