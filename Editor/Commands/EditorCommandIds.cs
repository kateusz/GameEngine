namespace Editor.Commands;

/// <summary>Stable command ids. Prefix matches menu / palette category.</summary>
public static class EditorCommandIds
{
    public const string EntityGotoPrefix = "entity.goto:";

    // Project
    public const string NewProject = "project.new";
    public const string OpenProject = "project.open";
    public const string CloseProject = "project.close";
    public const string ShowRecentProjects = "project.show-recent";
    public const string ShowProjectSettings = "project.settings";
    public const string Export = "project.export";

    // Scene
    public const string NewScene = "scene.new";
    public const string OpenScene = "scene.open";
    public const string SaveScene = "scene.save";
    public const string CloseScene = "scene.close";
    public const string SceneSettings = "scene.settings";

    // View
    public const string OpenCommandPalette = "view.command-palette";
    public const string ResetCamera = "view.reset-camera";
    public const string ToggleRulers = "view.toggle-rulers";
    public const string ToggleStats = "view.toggle-stats";

    // Editor
    public const string ShowEditorSettings = "editor.settings";

    // Help
    public const string ShowKeyboardShortcuts = "help.keyboard-shortcuts";

    // Tools (palette / shortcuts only)
    public const string SelectTool = "tools.select";
    public const string MoveTool = "tools.move";
    public const string ScaleTool = "tools.scale";
    public const string RulerTool = "tools.ruler";

    // Edit (palette / shortcuts only)
    public const string DuplicateEntity = "edit.duplicate-entity";
    public const string DeleteEntity = "edit.delete-entity";
    public const string Undo = "edit.undo";
    public const string Redo = "edit.redo";

    public static string EntityGoto(int entityId) => EntityGotoPrefix + entityId;
}
