using Editor.Panels;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine;

namespace Editor.Features.Project;

public class NewProjectPopup
{
    private readonly IProjectManager _projectManager;
    private readonly IContentBrowserPanel _contentBrowserPanel;
    private readonly IAssetsManager _assetsManager;

    private bool _showNewProjectPopup;
    private bool _showOpenProjectPopup;

    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string _openProjectPath = string.Empty;
    private string _openProjectError = string.Empty;

    public NewProjectPopup(IProjectManager projectManager, IContentBrowserPanel contentBrowserPanel, IAssetsManager assetsManager)
    {
        _projectManager = projectManager;
        _contentBrowserPanel = contentBrowserPanel;
        _assetsManager = assetsManager;
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
        bool isValid = _projectManager.IsValidProjectName(_newProjectName);
        string? validationMessage = (!isValid && !string.IsNullOrEmpty(_newProjectName))
            ? "Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores."
            : null;

        ModalDrawer.RenderInputModal(
            title: "New Project",
            showModal: ref _showNewProjectPopup,
            promptText: "Enter Project Name:",
            inputValue: ref _newProjectName,
            maxLength: EditorUIConstants.MaxNameLength,
            validationMessage: validationMessage,
            errorMessage: _newProjectError,
            isValid: isValid,
            onOk: () =>
            {
                if (_projectManager.TryCreateNewProject(_newProjectName?.Trim() ?? string.Empty, out var err))
                {
                    _newProjectName = string.Empty;
                    _newProjectError = string.Empty;
                    _contentBrowserPanel.SetRootDirectory(_assetsManager.AssetsPath);
                }
                else
                {
                    _newProjectError = err;
                    _showNewProjectPopup = true; // Keep modal open on error
                }
            },
            onCancel: () =>
            {
                _newProjectName = string.Empty;
                _newProjectError = string.Empty;
            },
            okLabel: "Create");
    }

    private void RenderOpenProjectPopup()
    {
        bool hasInput = !string.IsNullOrWhiteSpace(_openProjectPath);

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
                if (_projectManager.TryOpenProject(_openProjectPath?.Trim() ?? string.Empty, out var err))
                {
                    _openProjectPath = string.Empty;
                    _openProjectError = string.Empty;
                    _contentBrowserPanel.SetRootDirectory(_assetsManager.AssetsPath);
                }
                else
                {
                    _openProjectError = err;
                    _showOpenProjectPopup = true; // Keep modal open on error
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
