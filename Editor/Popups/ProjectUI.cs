using System.Numerics;
using Editor.Managers;
using ImGuiNET;
using Editor.UI;
using Engine;

namespace Editor.Panels;

public class ProjectUI
{
    private readonly IProjectManager _projectManager;
    private readonly IContentBrowserPanel _contentBrowserPanel;

    private bool _showNewProjectPopup;
    private bool _showOpenProjectPopup;

    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string _openProjectPath = string.Empty;
    private string _openProjectError = string.Empty;

    public ProjectUI(IProjectManager projectManager, IContentBrowserPanel contentBrowserPanel)
    {
        _projectManager = projectManager;
        _contentBrowserPanel = contentBrowserPanel;
    }

    public void ShowNewProjectPopup() => _showNewProjectPopup = true;
    public void ShowOpenProjectPopup() => _showOpenProjectPopup = true;

    public void Render()
    {
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
    }

    private void RenderNewProjectPopup()
    {
        if (_showNewProjectPopup)
            ImGui.OpenPopup("New Project");

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("New Project", ref _showNewProjectPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Name:");
            ImGui.InputText("##ProjectName", ref _newProjectName, EditorUIConstants.MaxNameLength);
            ImGui.Separator();

            bool isValid = _projectManager.IsValidProjectName(_newProjectName);

            if (!isValid && !string.IsNullOrEmpty(_newProjectName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped("Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }
            if (!string.IsNullOrEmpty(_newProjectError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_newProjectError);
                ImGui.PopStyleColor();
            }

            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Create", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                if (_projectManager.TryCreateNewProject(_newProjectName?.Trim() ?? string.Empty, out var err))
                {
                    _showNewProjectPopup = false;
                    _newProjectName = string.Empty;
                    _newProjectError = string.Empty;

                    _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);
                }
                else
                {
                    _newProjectError = err;
                }
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                _showNewProjectPopup = false;
                _newProjectName = string.Empty;
                _newProjectError = string.Empty;
            }

            ImGui.EndPopup();
        }
    }

    private void RenderOpenProjectPopup()
    {
        if (_showOpenProjectPopup)
            ImGui.OpenPopup("Open Project");

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Open Project", ref _showOpenProjectPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Path:");
            ImGui.InputText("##OpenProjectName", ref _openProjectPath, EditorUIConstants.MaxNameLength);
            ImGui.Separator();

            if (!string.IsNullOrEmpty(_openProjectError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_openProjectError);
                ImGui.PopStyleColor();
            }

            bool hasInput = !string.IsNullOrWhiteSpace(_openProjectPath);

            ImGui.BeginDisabled(!hasInput);
            if (ImGui.Button("Open", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                if (_projectManager.TryOpenProject(_openProjectPath?.Trim() ?? string.Empty, out var err))
                {
                    _showOpenProjectPopup = false;
                    _openProjectPath = string.Empty;
                    _openProjectError = string.Empty;

                    _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);
                }
                else
                {
                    _openProjectError = err;
                }
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                _showOpenProjectPopup = false;
                _openProjectPath = string.Empty;
                _openProjectError = string.Empty;
            }

            ImGui.EndPopup();
        }
    }
}
