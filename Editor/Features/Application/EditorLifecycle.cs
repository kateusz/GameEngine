using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Input;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Scene;
using Serilog;

namespace Editor.Features.Application;

public class EditorLifecycle(
    IEnumerable<IEditorLifecycleListener> listeners,
    IEditorPreferences editorPreferences,
    DebugSettings debugSettings,
    ISceneContext sceneContext,
    ISceneManager sceneManager,
    ShortcutManager shortcutManager,
    EditorShortcutRegistrar shortcutRegistrar,
    IEditorViewport editorViewport,
    IContentBrowserPanel contentBrowserPanel,
    SceneToolbar sceneToolbar,
    IConsolePanel consolePanel)
{
    private static readonly ILogger Logger = Log.ForContext<EditorLifecycle>();

    public void Attach(IInputSystem inputSystem)
    {
        Logger.Debug("EditorLifecycle Attach.");

        foreach (var listener in listeners)
            listener.Attach();

        editorViewport.Initialize();
        sceneManager.New("");
        contentBrowserPanel.Init();
        sceneToolbar.Init();

        debugSettings.ShowColliderBounds = editorPreferences.ShowColliderBounds;
        debugSettings.ShowFPS = editorPreferences.ShowFPS;

        shortcutRegistrar.RegisterAll(shortcutManager);

        Logger.Information("Editor initialized successfully!");
        Logger.Information("Console panel is now capturing output.");
    }

    public void Detach()
    {
        Logger.Debug("EditorLifecycle Detach.");

        foreach (var listener in listeners)
            listener.Detach();

        sceneContext.ActiveScene?.Dispose();
        editorViewport.Dispose();
        consolePanel.Dispose();
    }
}
