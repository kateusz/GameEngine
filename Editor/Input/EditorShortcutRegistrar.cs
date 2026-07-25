using Editor.Features.History;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Engine.Scene;
using Input;
using Serilog;

namespace Editor.Input;

public class EditorShortcutRegistrar(
    ViewportComponents viewport,
    SceneSettingsPopup sceneSettingsPopup,
    ISceneManager sceneManager,
    IEditorSelection selection,
    IEditorCameraController cameraController,
    IEditorHistory history,
    ISceneContext sceneContext)
{
    private static readonly ILogger Logger = Log.ForContext<EditorShortcutRegistrar>();

    public void RegisterAll(ShortcutManager shortcutManager)
    {
        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Q, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Select,
            "Select tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.W, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Move,
            "Move tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Scale,
            "Scale tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.E, KeyModifiers.ShiftOnly,
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Ruler,
            "Ruler tool", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Escape, KeyModifiers.None,
            () =>
            {
                if (viewport.SceneToolbar.CurrentMode == EditorMode.Ruler)
                {
                    var rulerTool = viewport.ViewportToolManager.GetTool<RulerTool>();
                    rulerTool?.ClearMeasurement();
                }
            },
            "Clear ruler measurement", "Tools"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.N, KeyModifiers.CtrlOnly,
            sceneSettingsPopup.ShowNewScenePopup,
            "New scene", "File"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly,
            () => sceneManager.Save(),
            "Save scene", "File"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.D, KeyModifiers.CtrlOnly,
            () =>
            {
                if (selection.SelectedEntity is { } entity)
                    sceneManager.DuplicateEntity(entity);
            },
            "Duplicate entity", "Edit"));
        
        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Z, KeyModifiers.CtrlOnly,
            () =>
            {
                if (sceneContext.State == SceneState.Edit)
                    history.Undo();
            },
            "Undo", "Edit"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.Y, KeyModifiers.CtrlOnly,
            () =>
            {
                if (sceneContext.State == SceneState.Edit)
                    history.Redo();
            },
            "Redo", "Edit"));

        shortcutManager.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.R, KeyModifiers.CtrlOnly,
            cameraController.ResetCamera,
            "Reset camera", "Navigation"));

        Logger.Debug("Registered {Count} keyboard shortcuts", shortcutManager.Shortcuts.Count);
    }
}
