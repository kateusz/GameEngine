using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Features.Project;

public class NewProjectPopup(IProjectManager projectManager)
{
    private bool _showNewProjectPopup;
    private bool _showOpenProjectPopup;

    private string _newProjectParentPath = Environment.CurrentDirectory;
    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string _openProjectPath = string.Empty;
    private string _openProjectError = string.Empty;

    public void ShowNewProjectPopup() => _showNewProjectPopup = true;
    public void ShowOpenProjectPopup() => _showOpenProjectPopup = true;

    public void Render()
    {
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
    }

    private void RenderNewProjectPopup()
    {
        const string title = "New Project";
        if (_showNewProjectPopup)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (!ImGui.BeginPopupModal(title, ref _showNewProjectPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            return;

        ImGui.Text("Parent folder (project will be created inside this folder):");
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();
        ImGui.InputText("##NewProject_Parent", ref _newProjectParentPath, EditorUIConstants.MaxPathLength);

        ImGui.Spacing();
        ImGui.Text("Project name (new subfolder name):");
        var enterOnName = ImGui.InputText("##NewProject_Name", ref _newProjectName, EditorUIConstants.MaxNameLength,
            ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.Separator();

        var validation = GetNewProjectValidationMessage();
        if (!string.IsNullOrEmpty(validation))
            DrawValidationLine(validation);

        if (!string.IsNullOrEmpty(_newProjectError))
            DrawValidationLine(_newProjectError);

        var canCreate = string.IsNullOrEmpty(validation) &&
                        projectManager.IsValidProjectName(_newProjectName) &&
                        !string.IsNullOrWhiteSpace(_newProjectParentPath);

        var shouldExecuteOk = enterOnName && canCreate;
        var shouldClose = false;
        var actionExecuted = false;

        ButtonDrawer.DrawModalButtonPair(
            okLabel: "Create",
            cancelLabel: "Cancel",
            onOk: () =>
            {
                if (!actionExecuted && canCreate)
                {
                    shouldClose = true;
                    actionExecuted = true;
                    if (projectManager.TryCreateNewProject(
                            _newProjectParentPath.Trim(),
                            _newProjectName.Trim(),
                            out var err))
                    {
                        _newProjectName = string.Empty;
                        _newProjectError = string.Empty;
                    }
                    else
                    {
                        _newProjectError = err;
                        shouldClose = false;
                    }
                }
            },
            onCancel: () =>
            {
                if (!actionExecuted)
                {
                    shouldClose = true;
                    actionExecuted = true;
                    _newProjectName = string.Empty;
                    _newProjectError = string.Empty;
                }
            },
            okDisabled: !canCreate);

        if (shouldExecuteOk && !actionExecuted)
        {
            actionExecuted = true;
            if (projectManager.TryCreateNewProject(
                    _newProjectParentPath.Trim(),
                    _newProjectName.Trim(),
                    out var err))
            {
                _newProjectName = string.Empty;
                _newProjectError = string.Empty;
                shouldClose = true;
            }
            else
                _newProjectError = err;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape) && !actionExecuted)
        {
            shouldClose = true;
            actionExecuted = true;
            _newProjectName = string.Empty;
            _newProjectError = string.Empty;
        }

        if (shouldClose)
            _showNewProjectPopup = false;

        ImGui.EndPopup();
    }

    private string? GetNewProjectValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(_newProjectParentPath))
            return "Parent folder path is required.";

        var parentFull = Path.GetFullPath(Path.IsPathRooted(_newProjectParentPath.Trim())
            ? _newProjectParentPath.Trim()
            : Path.Combine(Environment.CurrentDirectory, _newProjectParentPath.Trim()));

        if (!Directory.Exists(parentFull))
            return "Parent folder does not exist.";

        if (string.IsNullOrEmpty(_newProjectName))
            return null;

        if (!projectManager.IsValidProjectName(_newProjectName))
            return "Project name must contain only letters, numbers, spaces, dashes, or underscores.";

        var projectDir = Path.GetFullPath(Path.Combine(parentFull, _newProjectName.Trim()));
        if (Directory.Exists(projectDir))
            return "A folder with this name already exists in the selected location.";

        return null;
    }

    private static void DrawValidationLine(string message)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.35f, 0.35f, 1f));
        ImGui.TextWrapped(message);
        ImGui.PopStyleColor();
    }

    private void RenderOpenProjectPopup()
    {
        var hasInput = !string.IsNullOrWhiteSpace(_openProjectPath);

        ModalDrawer.RenderInputModal(
            title: "Open Project",
            showModal: ref _showOpenProjectPopup,
            promptText: "Enter Project Path:",
            inputValue: ref _openProjectPath,
            maxLength: EditorUIConstants.MaxPathLength,
            validationMessage: null,
            errorMessage: _openProjectError,
            isValid: hasInput,
            onOk: () =>
            {
                if (projectManager.TryOpenProject(_openProjectPath?.Trim() ?? string.Empty, out var err))
                {
                    _openProjectPath = string.Empty;
                    _openProjectError = string.Empty;
                }
                else
                {
                    _openProjectError = err;
                    _showOpenProjectPopup = true;
                }
            },
            onCancel: () =>
            {
                _openProjectPath = string.Empty;
                _openProjectError = string.Empty;
            },
            okLabel: "Open");
    }
}
