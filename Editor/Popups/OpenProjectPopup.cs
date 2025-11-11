using System.Numerics;
using Editor.Core;
using Editor.Managers;
using Editor.Panels;
using Editor.UI;
using Engine;
using ImGuiNET;
using Serilog;

namespace Editor.Popups;

public class OpenProjectPopup : IEditorPopup
{
    private static readonly ILogger Logger = Log.ForContext<OpenProjectPopup>();

    private readonly IProjectManager _projectManager;
    private readonly IContentBrowserPanel _contentBrowserPanel;
    private readonly EditorEventBus _eventBus;

    private string _projectPath = string.Empty;
    private string _errorMessage = string.Empty;

    // IEditorPopup implementation
    public string Id => "OpenProject";
    public bool IsOpen { get; private set; }

    public OpenProjectPopup(IProjectManager projectManager, IContentBrowserPanel contentBrowserPanel, EditorEventBus eventBus)
    {
        _projectManager = projectManager;
        _contentBrowserPanel = contentBrowserPanel;
        _eventBus = eventBus;
    }

    public void Show()
    {
        IsOpen = true;
        _projectPath = string.Empty;
        _errorMessage = string.Empty;
    }

    public void OnImGuiRender()
    {
        if (IsOpen)
            ImGui.OpenPopup(Id);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var isOpen = IsOpen;
        if (ImGui.BeginPopupModal("Open Project", ref isOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Path:");
            ImGui.InputText("##OpenProjectPath", ref _projectPath, EditorUIConstants.MaxPathLength);
            ImGui.Separator();

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_errorMessage);
                ImGui.PopStyleColor();
            }

            bool hasInput = !string.IsNullOrWhiteSpace(_projectPath);

            ImGui.BeginDisabled(!hasInput);
            if (ImGui.Button("Open", new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                if (_projectManager.TryOpenProject(_projectPath?.Trim() ?? string.Empty, out var err))
                {
                    IsOpen = false;
                    var projectName = Path.GetFileName(_projectPath?.Trim() ?? string.Empty);
                    _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);

                    // Publish event
                    _eventBus.Publish(new ProjectOpenedEvent(_projectPath, projectName));

                    Logger.Information("Opened project: {ProjectPath}", _projectPath);

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
        _projectPath = string.Empty;
        _errorMessage = string.Empty;
    }
}
