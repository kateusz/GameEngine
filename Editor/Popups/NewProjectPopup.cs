using System.Numerics;
using Editor.Core;
using Editor.Managers;
using Editor.Panels;
using Editor.UI;
using Engine;
using ImGuiNET;

namespace Editor.Popups;

public class NewProjectPopup : IEditorPopup
{
    private readonly IProjectManager _projectManager;
    private readonly IContentBrowserPanel _contentBrowserPanel;
    private readonly EditorEventBus _eventBus;

    private string _projectName = string.Empty;
    private string _errorMessage = string.Empty;

    // IEditorPopup implementation
    public string Id => "NewProject";
    public bool IsOpen { get; private set; }

    public NewProjectPopup(IProjectManager projectManager, IContentBrowserPanel contentBrowserPanel, EditorEventBus eventBus)
    {
        _projectManager = projectManager;
        _contentBrowserPanel = contentBrowserPanel;
        _eventBus = eventBus;
    }

    public void Show()
    {
        IsOpen = true;
        _projectName = string.Empty;
        _errorMessage = string.Empty;
    }

    public void OnImGuiRender()
    {
        if (IsOpen)
            ImGui.OpenPopup(Id);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var isOpen = IsOpen;
        if (ImGui.BeginPopupModal("New Project", ref isOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Name:");
            ImGui.InputText("##ProjectName", ref _projectName, EditorUIConstants.MaxNameLength);
            ImGui.Separator();

            bool isValid = _projectManager.IsValidProjectName(_projectName);

            if (!isValid && !string.IsNullOrEmpty(_projectName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped("Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_errorMessage);
                ImGui.PopStyleColor();
            }

            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Create", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                if (_projectManager.TryCreateNewProject(_projectName?.Trim() ?? string.Empty, out var err))
                {
                    IsOpen = false;
                    _projectName = string.Empty;
                    _errorMessage = string.Empty;

                    _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);

                    // Publish event
                    _eventBus.Publish(new ProjectCreatedEvent(AssetsManager.AssetsPath, _projectName));

                    OnClose();
                }
                else
                {
                    _errorMessage = err;
                }
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                IsOpen = false;
                OnClose();
            }

            ImGui.EndPopup();
        }

        IsOpen = isOpen;
    }

    public void OnClose()
    {
        _projectName = string.Empty;
        _errorMessage = string.Empty;
    }
}
