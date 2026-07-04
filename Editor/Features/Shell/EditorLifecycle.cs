using ECS;
using Editor.Features.Project;
using Editor.Features.Scene;
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
    IEditorPreferences editorPreferences,
    DebugSettings debugSettings,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    IScriptEngine scriptEngine,
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

    public void Attach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLifecycle Attach.");

        _sceneChangedHandler = newScene =>
        {
            panels.SceneHierarchyPanel.SetScene(newScene);

            if (string.IsNullOrWhiteSpace(newScene.Name))
                return;

            var scriptsDir = projectManager.ScriptsDir ??
                             Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
            scriptEngine.SetScriptsDirectory(scriptsDir);

#if DEBUG
            scriptEngine.EnableHybridDebugging(true);

            var symbolsPath = Path.Combine(Environment.CurrentDirectory, "DebugSymbols", "Scripts");
            Directory.CreateDirectory(symbolsPath);
            scriptEngine.SaveDebugSymbols(Path.Combine(symbolsPath, "GameAssembly"), "GameAssembly");
            scriptEngine.PrintDebugInfo();
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
