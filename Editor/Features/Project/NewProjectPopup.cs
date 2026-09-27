using System.Numerics;
using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Platform;
using ImGuiNET;
using Serilog;

namespace Editor.Features.Project;

public class NewProjectPopup(IProjectManager projectManager)
{
    private static readonly ILogger Logger = Log.ForContext<NewProjectPopup>();

    private const float LabelWidth = 110f;

    private bool _showNewProjectPopup;
    private bool _showOpenProjectPopup;

    private string _newProjectParentPath =
        OSInfo.IsWindows ? string.Empty : Environment.CurrentDirectory;
    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string _openProjectPath = string.Empty;
    private string _openProjectError = string.Empty;

    public void ShowNewProjectPopup() => _showNewProjectPopup = true;

    public bool ShowOpenProjectPopup()
    {
        if (OSInfo.IsWindows)
        {
            var path = FolderPicker.PickFolder("Select Project Folder", Environment.CurrentDirectory);
            if (string.IsNullOrEmpty(path))
                return false;

            if (projectManager.TryOpenProject(path, out var err))
                return true;

            Logger.Warning("Failed to open project {Path}: {Error}", path, err);
            return false;
        }

        _showOpenProjectPopup = true;
        return false;
    }

    public void Render()
    {
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
    }

    private void RenderNewProjectPopup()
    {
        if (!_showNewProjectPopup)
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(520f, 0f), new Vector2(float.MaxValue, float.MaxValue));

        if (!ModalDrawer.BeginCenteredModal(
                "New Project",
                ref _showNewProjectPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings))
            return;

        DrawLabel("Project Name");
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();
        var enterOnName = ImGui.InputText(
            "##NewProject_Name",
            ref _newProjectName,
            EditorUIConstants.MaxNameLength,
            ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.Spacing();
        DrawLabel("Location");
        LayoutDrawer.DrawPathField("NewProject_Parent", ref _newProjectParentPath, () =>
        {
            var initial = !string.IsNullOrWhiteSpace(_newProjectParentPath) && Directory.Exists(_newProjectParentPath)
                ? _newProjectParentPath
                : Environment.CurrentDirectory;
            var picked = FolderPicker.PickFolder(
                "Select Folder Where Project Will Be Created",
                initial);
            if (!string.IsNullOrEmpty(picked))
                _newProjectParentPath = picked;
        });

        if (!string.IsNullOrWhiteSpace(_newProjectParentPath) &&
            !string.IsNullOrWhiteSpace(_newProjectName) &&
            projectManager.IsValidProjectName(_newProjectName))
        {
            ImGui.Spacing();
            DrawLabel("Project path");
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.SuccessColor);
            ImGui.TextWrapped(Path.Combine(_newProjectParentPath.Trim(), _newProjectName.Trim()));
            ImGui.PopStyleColor();
        }

        LayoutDrawer.DrawSeparatorWithSpacing();

        var validation = GetNewProjectValidationMessage();
        var missingFolder = string.IsNullOrWhiteSpace(_newProjectParentPath);

        // Missing-folder hint lives on the Create tooltip; keep other validation inline.
        if (!string.IsNullOrEmpty(validation) && !missingFolder)
            DrawValidationLine(validation);

        if (!string.IsNullOrEmpty(_newProjectError))
            DrawValidationLine(_newProjectError);

        var canCreate = string.IsNullOrEmpty(validation) &&
                        projectManager.IsValidProjectName(_newProjectName) &&
                        !missingFolder;

        var shouldExecuteOk = enterOnName && canCreate;
        var shouldClose = false;
        var actionExecuted = false;

        var buttonWidth = EditorUIConstants.StandardButtonWidth;
        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - buttonWidth) * 0.5f);
        if (ButtonDrawer.DrawModalButton("Create", disabled: !canCreate) && !actionExecuted && canCreate)
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

        if (!canCreate && !string.IsNullOrEmpty(validation) &&
            ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(validation);
            ImGui.EndTooltip();
        }

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

        ModalDrawer.EndModal();
    }

    private static void DrawLabel(string label)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(LabelWidth);
    }

    private string? GetNewProjectValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(_newProjectParentPath))
            return OSInfo.IsWindows
                ? "Select a folder where the project will be created."
                : "Parent folder path is required.";

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
        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
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
