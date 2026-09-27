using Editor.Features.History;
using Editor.Features.Scripting;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Input;
using Editor.Panels;
using Engine.Core;
using Engine.Project;
using Engine.Renderer.Models;
using Engine.Scene;
using Serilog;

namespace Editor.Features.Application;

public class EditorLifecycle(
    IProjectManager projectManager,
    IProjectContext projectContext,
    IEditorPreferences editorPreferences,
    DebugSettings debugSettings,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    EditorSceneLoadService sceneLoadService,
    GameScriptWorkspace scriptWorkspace,
    ShortcutManager shortcutManager,
    EditorShortcutRegistrar shortcutRegistrar,
    IEditorHistory history,
    IEditorViewport editorViewport,
    SceneHierarchyPanel sceneHierarchyPanel,
    ContentBrowserPanel contentBrowserPanel,
    IConsolePanel consolePanel,
    ViewportComponents viewport,
    IModelFactory modelFactory)
{
    private static readonly ILogger Logger = Log.ForContext<EditorLifecycle>();

    private Action<IScene> _sceneChangedHandler = null!;
    private Action _projectOpenedHandler = null!;
    private Action _projectClosingHandler = null!;
    private Action _projectClosedHandler = null!;

    public void Attach()
    {
        Logger.Debug("EditorLifecycle Attach.");

        _projectClosingHandler = () =>
        {
            if (sceneContext.State == SceneState.Play)
                sceneManager.Stop();
            else
                sceneContext.ActiveScene?.Dispose();

            scriptWorkspace.RevokeAndUnload();
            modelFactory.Clear();
        };

        _projectOpenedHandler = () =>
        {
            contentBrowserPanel.SetRootDirectory(projectContext.AssetsPath);
            TryLoadDefaultScene();
        };

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
            history.Clear();
        };
        sceneContext.SceneChanged += _sceneChangedHandler;

        editorViewport.Initialize();
        sceneHierarchyPanel.Initialize();

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
        sceneContext.ActiveScene?.Dispose();
        editorViewport.Dispose();
        contentBrowserPanel.Dispose();
        consolePanel?.Dispose();
    }

    private void TryLoadDefaultScene()
    {
        if (projectContext.Root is null)
            return;

        var configPath = GameConfiguration.PathFor(projectContext.Root);
        if (!GameConfiguration.TryLoad(configPath, out var config, out _))
            return;

        if (string.IsNullOrWhiteSpace(config.StartupScenePath))
            return;

        var scenePath = Path.GetFullPath(Path.Combine(projectContext.Root, config.StartupScenePath));
        if (!File.Exists(scenePath))
        {
            Logger.Warning("Default scene not found: {ScenePath}", scenePath);
            return;
        }

        sceneLoadService.Request(scenePath);
    }
}
