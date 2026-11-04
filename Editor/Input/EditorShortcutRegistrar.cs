using Editor.Commands;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Panels;
using Editor.Publisher;
using Engine.Project;
using Engine.Scene;
using Input;
using Serilog;

namespace Editor.Input;

public class EditorShortcutRegistrar(
    CommandRegistry registry,
    CommandPalette commandPalette,
    ViewportComponents viewport,
    SceneSettingsPopup sceneSettingsPopup,
    ISceneManager sceneManager,
    IEditorSelection selection,
    IEditorCameraController cameraController,
    IEditorHistory history,
    ISceneContext sceneContext,
    IProjectManager projectManager,
    IProjectContext projectContext,
    NewProjectPopup newProjectPopup,
    RecentProjectsPanel recentProjectsPanel,
    RendererStatsPanel rendererStatsPanel,
    KeyboardShortcutsPanel keyboardShortcutsPanel,
    EditorSettingsUI editorSettingsUI,
    ProjectSettingsUI projectSettingsUI,
    PublishSettingsUI publishSettingsUI,
    UnsavedSceneGuard unsavedSceneGuard)
{
    private static readonly ILogger Logger = Log.ForContext<EditorShortcutRegistrar>();

    public void RegisterAll(ShortcutManager shortcutManager)
    {
        CanExecuteResult EditMode() =>
            sceneContext.State == SceneState.Edit
                ? CanExecuteResult.Yes
                : CanExecuteResult.No("Only available in Edit mode");

        CanExecuteResult EditWithSelection() =>
            sceneContext.State != SceneState.Edit
                ? CanExecuteResult.No("Only available in Edit mode")
                : selection.SelectedEntity is null
                    ? CanExecuteResult.No("No entity selected")
                    : CanExecuteResult.Yes;

        CanExecuteResult HasProject() =>
            projectContext.HasProject
                ? CanExecuteResult.Yes
                : CanExecuteResult.No("No project open");

        Register(shortcutManager, EditorCommandIds.OpenCommandPalette, "Command Palette", "View",
            commandPalette.Show, key: KeyCodes.P, modifiers: KeyModifiers.CtrlShift);

        Register(shortcutManager, EditorCommandIds.SelectTool, "Select", "Tools",
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Select,
            key: KeyCodes.Q, modifiers: KeyModifiers.ShiftOnly);
        Register(shortcutManager, EditorCommandIds.MoveTool, "Move", "Tools",
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Move,
            key: KeyCodes.W, modifiers: KeyModifiers.ShiftOnly);
        Register(shortcutManager, EditorCommandIds.ScaleTool, "Scale", "Tools",
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Scale,
            key: KeyCodes.R, modifiers: KeyModifiers.ShiftOnly);
        Register(shortcutManager, EditorCommandIds.RulerTool, "Ruler", "Tools",
            () => viewport.SceneToolbar.CurrentMode = EditorMode.Ruler,
            key: KeyCodes.E, modifiers: KeyModifiers.ShiftOnly);

        Register(shortcutManager, EditorCommandIds.NewScene, "New...", "Scene",
            sceneSettingsPopup.ShowNewScenePopup,
            key: KeyCodes.N, modifiers: KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.OpenScene, "Open...", "Scene",
            sceneSettingsPopup.ShowOpenScenePopup, HasProject, key: KeyCodes.O, modifiers: KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.SaveScene, "Save", "Scene",
            () => sceneManager.Save(),
            key: KeyCodes.S, modifiers: KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.CloseScene, "Close", "Scene",
            () =>
            {
                if (sceneManager.IsDirty)
                    sceneSettingsPopup.ShowCloseConfirmation();
                else
                    sceneManager.Close();
            });
        Register(shortcutManager, EditorCommandIds.SceneSettings, "Settings...", "Scene",
            sceneSettingsPopup.ShowSettings);

        Register(shortcutManager, EditorCommandIds.DuplicateEntity, "Duplicate entity", "Edit",
            () =>
            {
                if (selection.SelectedEntity is { } entity)
                    sceneContext.ActiveScene?.DuplicateEntity(entity);
            },
            EditWithSelection, KeyCodes.D, KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.DeleteEntity, "Delete entity", "Edit",
            () =>
            {
                if (selection.SelectedEntity is not { } entity || sceneContext.ActiveScene is not { } scene)
                    return;
                history.Execute(new DestroyEntitySubtreeCommand(scene, entity.Id));
            },
            EditWithSelection, KeyCodes.Delete, KeyModifiers.None);
        Register(shortcutManager, EditorCommandIds.Undo, "Undo", "Edit",
            () => history.Undo(), EditMode, KeyCodes.Z, KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.Redo, "Redo", "Edit",
            () => history.Redo(), EditMode, KeyCodes.Y, KeyModifiers.CtrlOnly);

        Register(shortcutManager, EditorCommandIds.ResetCamera, "Reset camera", "View",
            cameraController.ResetCamera, key: KeyCodes.R, modifiers: KeyModifiers.CtrlOnly);
        Register(shortcutManager, EditorCommandIds.ToggleRulers, "Toggle rulers", "View",
            () => viewport.ViewportRuler.Enabled = !viewport.ViewportRuler.Enabled);
        Register(shortcutManager, EditorCommandIds.ToggleStats, "Toggle debug", "View",
            () => rendererStatsPanel.IsVisible = !rendererStatsPanel.IsVisible);
        Register(shortcutManager, EditorCommandIds.ShowEditorSettings, "Settings...", "Editor",
            editorSettingsUI.Show);
        Register(shortcutManager, EditorCommandIds.ShowKeyboardShortcuts, "Keyboard Shortcuts...", "Help",
            keyboardShortcutsPanel.Show);

        Register(shortcutManager, EditorCommandIds.NewProject, "New...", "Project",
            () => unsavedSceneGuard.Run(newProjectPopup.ShowNewProjectPopup));
        Register(shortcutManager, EditorCommandIds.OpenProject, "Open...", "Project",
            () => unsavedSceneGuard.Run(() => newProjectPopup.ShowOpenProjectPopup()));
        Register(shortcutManager, EditorCommandIds.CloseProject, "Close", "Project",
            () => unsavedSceneGuard.Run(() =>
            {
                projectManager.CloseProject();
                recentProjectsPanel.Show();
            }),
            HasProject);
        Register(shortcutManager, EditorCommandIds.ShowRecentProjects, "Show Recent Projects", "Project",
            recentProjectsPanel.Show);

        Register(shortcutManager, EditorCommandIds.ShowProjectSettings, "Settings...", "Project",
            projectSettingsUI.Show, HasProject);
        Register(shortcutManager, EditorCommandIds.Export, "Export...", "Project",
            publishSettingsUI.ShowExportModal, HasProject);

        Logger.Debug("Registered {Count} commands and {Shortcuts} shortcuts",
            registry.GetWorkingSet().Count(c => !c.Id.StartsWith(EditorCommandIds.EntityGotoPrefix)),
            shortcutManager.Shortcuts.Count);
    }

    private void Register(
        ShortcutManager shortcutManager,
        string id,
        string title,
        string category,
        Action execute,
        Func<CanExecuteResult>? canExecute = null,
        KeyCodes? key = null,
        KeyModifiers? modifiers = null)
    {
        if (key is { } keyCode)
        {
            var chord = new KeyboardShortcut(
                keyCode,
                modifiers ?? KeyModifiers.None,
                () => registry.Execute(id),
                title,
                category);
            var shortcutDisplay = chord.GetDisplayString();
            registry.Register(new EditorCommand(id, title, category, execute, canExecute, shortcutDisplay));
            shortcutManager.RegisterShortcut(chord);
            return;
        }

        registry.Register(new EditorCommand(id, title, category, execute, canExecute));
    }
}