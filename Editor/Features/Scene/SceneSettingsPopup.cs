using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Features.Scene;

/// <summary>
/// Handles scene-related UI popups and modals in the editor.
/// </summary>
public class SceneSettingsPopup(
    ISceneManager sceneManager,
    ISceneContext sceneContext,
    IProjectContext projectContext,
    EditorSceneLoadService sceneLoadService)
{
    private bool _showNewScenePopup;
    private bool _showCloseConfirmation;
    private bool _showSettings;
    private bool _showOpenScene;
    private string _newSceneName = string.Empty;
    private string _newSceneError = string.Empty;
    private string[] _openScenePaths = [];
    private int _openSceneSelected = -1;

    public void ShowNewScenePopup() => _showNewScenePopup = true;

    public void ShowCloseConfirmation() => _showCloseConfirmation = true;

    public void ShowSettings() => _showSettings = true;

    public void ShowOpenScenePopup()
    {
        _openScenePaths = EnumerateProjectScenes();
        _openSceneSelected = _openScenePaths.Length > 0 ? 0 : -1;
        _showOpenScene = true;
    }

    public void Render()
    {
        RenderNewScenePopup();
        RenderCloseConfirmationModal();
        RenderSettingsModal();
        RenderOpenSceneModal();
    }

    private void RenderOpenSceneModal()
    {
        if (!ModalDrawer.BeginCenteredModal("Open Scene", ref _showOpenScene))
            return;

        if (projectContext.Root is null)
        {
            ImGui.TextUnformatted("No project open.");
            if (ButtonDrawer.DrawModalButton("Close"))
                _showOpenScene = false;
            ModalDrawer.EndModal();
            return;
        }

        if (_openScenePaths.Length == 0)
        {
            ImGui.TextUnformatted("No .scene files in this project.");
            if (ButtonDrawer.DrawModalButton("Close"))
                _showOpenScene = false;
            ModalDrawer.EndModal();
            return;
        }

        var current = _openSceneSelected >= 0 ? _openScenePaths[_openSceneSelected] : _openScenePaths[0];
        ImGui.TextUnformatted("Scene:");
        ImGui.SameLine();
        LayoutDrawer.DrawComboBox(
            "##openScene",
            current,
            _openScenePaths,
            selected => _openSceneSelected = Array.IndexOf(_openScenePaths, selected),
            width: 300f);

        ImGui.Spacing();
        var buttonWidth = EditorUIConstants.StandardButtonWidth;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - buttonWidth) * 0.5f);
        if (ButtonDrawer.DrawModalButton("Open"))
            OpenSelectedScene();

        ModalDrawer.EndModal();
    }

    private void OpenSelectedScene()
    {
        if (_openSceneSelected < 0 || _openSceneSelected >= _openScenePaths.Length || projectContext.Root is null)
            return;

        var fullPath = Path.GetFullPath(Path.Combine(projectContext.Root, _openScenePaths[_openSceneSelected]));
        _showOpenScene = false;
        sceneLoadService.Request(fullPath);
    }

    private string[] EnumerateProjectScenes()
    {
        var root = projectContext.Root;
        var scenesDir = projectContext.ScenesDir;
        if (root is null || scenesDir is null || !Directory.Exists(scenesDir))
            return [];

        return Directory.EnumerateFiles(scenesDir, "*.scene", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void RenderSettingsModal()
    {
        if (!ModalDrawer.BeginCenteredModal("Scene Settings", ref _showSettings))
            return;

        if (sceneContext.ActiveScene is not { } scene)
        {
            ImGui.TextUnformatted("No scene open.");
            ModalDrawer.EndModal();
            return;
        }

        var backgroundColor = scene.BackgroundColor;
        if (LayoutDrawer.DrawColorEdit4("Background Color", ref backgroundColor))
        {
            scene.BackgroundColor = backgroundColor;
        }

        var skybox = scene.Skybox;
        if (ImGui.InputText("Skybox", ref skybox, 260))
        {
            scene.Skybox = string.IsNullOrWhiteSpace(skybox)
                ? ""
                : PathBuilder.ToAssetRelativePath(skybox.Trim());
        }

        ModalDrawer.EndModal();
    }

    private void RenderNewScenePopup()
    {
        var isValid = IsValidSceneName(_newSceneName);
        var validationMessage = (!isValid && !string.IsNullOrEmpty(_newSceneName))
            ? "Scene name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores."
            : null;

        ModalDrawer.RenderInputModal(
            title: "New Scene",
            showModal: ref _showNewScenePopup,
            promptText: "Scene Name:",
            inputValue: ref _newSceneName,
            maxLength: EditorUIConstants.MaxNameLength,
            onOk: () =>
            {
                try
                {
                    sceneManager.New(_newSceneName);
                    _newSceneName = string.Empty;
                    _newSceneError = string.Empty;
                }
                catch (Exception ex)
                {
                    _newSceneError = $"Failed to create scene: {ex.Message}";
                    _showNewScenePopup = true;
                }
            },
            onCancel: () =>
            {
                _newSceneName = string.Empty;
                _newSceneError = string.Empty;
            },
            new InputModalOptions(
                validationMessage: validationMessage,
                errorMessage: _newSceneError,
                isValid: isValid,
                okLabel: "Create"));
    }

    private void RenderCloseConfirmationModal()
    {
        const string title = "Close Scene";
        if (!ModalDrawer.BeginCenteredModal(title, ref _showCloseConfirmation))
            return;

        ImGui.TextWrapped("Save changes to the current scene before closing?");
        ImGui.Separator();

        if (ButtonDrawer.DrawModalButton("Save", () => { sceneManager.Save(); sceneManager.Close(); }))
            _showCloseConfirmation = false;
        ImGui.SameLine();
        if (ButtonDrawer.DrawModalButton("Don't Save", sceneManager.Close))
            _showCloseConfirmation = false;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            _showCloseConfirmation = false;

        ModalDrawer.EndModal();
    }

    private static bool IsValidSceneName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
    }
}
