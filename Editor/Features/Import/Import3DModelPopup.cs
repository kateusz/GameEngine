using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Platform;
using Serilog;

namespace Editor.Features.Import;

public class Import3DModelPopup(IProjectContext projectContext)
{
    private static readonly ILogger Logger = Log.ForContext<Import3DModelPopup>();

    private bool _showNoProject;
    private bool _showPathModal;
    private bool _showOverwrite;
    private bool _showSummary;

    private string _pathInput = OSInfo.IsWindows ? string.Empty : Environment.CurrentDirectory;
    private string _sourceDisplay = string.Empty;
    private IReadOnlyList<string> _pendingSources = [];
    private int _conflictCount;
    private string _summaryMessage = string.Empty;
    private MessageType _summaryType = MessageType.Info;

    public void Show()
    {
        ResetTransientUi();

        var noProject = projectContext.HasProject ? null : Import3DModelBatch.NoProjectError;
        if (noProject is not null)
        {
            _summaryMessage = noProject;
            _summaryType = MessageType.Error;
            _showNoProject = true;
            return;
        }

        if (OSInfo.IsWindows)
        {
            var picked = FolderPicker.PickFolder(
                "Select Folder Containing 3D Models",
                Environment.CurrentDirectory);
            if (string.IsNullOrEmpty(picked))
                return;

            BeginFromPath(picked);
            return;
        }

        _showPathModal = true;
    }

    public void Render()
    {
        if (_showNoProject)
        {
            ModalDrawer.RenderMessageBox(
                title: "Import 3D Model",
                showModal: ref _showNoProject,
                message: _summaryMessage,
                messageType: MessageType.Error);
            return;
        }

        if (_showPathModal)
        {
            RenderPathModal();
            return;
        }

        if (_showOverwrite)
        {
            ModalDrawer.RenderConfirmationModal(
                title: "Overwrite existing models?",
                showModal: ref _showOverwrite,
                message: Import3DModelBatch.FormatOverwriteMessage(_conflictCount),
                onOk: () => RunImport(overwriteConfirmed: true),
                onCancel: ResetTransientUi,
                okLabel: "Overwrite",
                cancelLabel: "Cancel");
            return;
        }

        if (_showSummary)
        {
            ModalDrawer.RenderMessageBox(
                title: "Import 3D Model",
                showModal: ref _showSummary,
                message: _summaryMessage,
                messageType: _summaryType,
                onClose: ResetTransientUi);
        }
    }

    private void RenderPathModal()
    {
        var hasInput = !string.IsNullOrWhiteSpace(_pathInput);

        ModalDrawer.RenderInputModal(
            title: "Import 3D Model",
            showModal: ref _showPathModal,
            promptText: "Enter file or folder path:",
            inputValue: ref _pathInput,
            maxLength: EditorUIConstants.MaxPathLength,
            validationMessage: null,
            errorMessage: null,
            isValid: hasInput,
            onOk: () => BeginFromPath(_pathInput.Trim()),
            onCancel: ResetTransientUi,
            okLabel: "OK");
    }

    private void BeginFromPath(string path)
    {
        _sourceDisplay = path;
        _showPathModal = false;

        if (string.IsNullOrWhiteSpace(path))
        {
            ShowSummaryError("Path is required.", MessageType.Warning);
            return;
        }

        var full = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(Environment.CurrentDirectory, path));

        if (!File.Exists(full) && !Directory.Exists(full))
        {
            ShowSummaryError("Path does not exist.", MessageType.Warning);
            return;
        }

        var sources = Import3DModelBatch.EnumerateSources(full);
        if (sources.Count == 0)
        {
            var msg =
                "No supported 3D model files found (.fbx, .glb, .gltf). " +
                (Directory.Exists(full)
                    ? "Pick a folder that contains models (non-recursive)."
                    : "File extension is not supported.");
            ShowSummaryError(msg, MessageType.Warning);
            return;
        }

        _pendingSources = sources;
        _sourceDisplay = full;

        var duplicates = Import3DModelBatch.FindDuplicateDestinations(
            sources, projectContext.AssetsPath);
        if (duplicates.Count > 0)
        {
            ShowSummaryError(
                Import3DModelBatch.FormatDuplicateDestinationMessage(duplicates),
                MessageType.Error);
            return;
        }

        _conflictCount = Import3DModelBatch.CountExistingDestinations(sources, projectContext.AssetsPath);
        if (_conflictCount > 0)
        {
            _showOverwrite = true;
            return;
        }

        RunImport(overwriteConfirmed: true);
    }

    private void ShowSummaryError(string message, MessageType type)
    {
        _summaryMessage = message;
        _summaryType = type;
        _showSummary = true;
    }

    private void RunImport(bool overwriteConfirmed)
    {
        _showOverwrite = false;

        if (!projectContext.HasProject)
        {
            _summaryMessage = Import3DModelBatch.NoProjectError;
            _summaryType = MessageType.Error;
            _showSummary = true;
            return;
        }

        var assetsRoot = projectContext.AssetsPath;

        var ran = Import3DModelBatch.TryImportBatch(
            _pendingSources,
            assetsRoot,
            overwriteConfirmed,
            out var summary);

        if (!ran || summary is null)
        {
            ResetTransientUi();
            return;
        }

        var result = summary.Value;
        Logger.Information(
            "Import 3D Model batch complete: ok={Ok} fail={Fail} source={Source}",
            result.Succeeded, result.Failures.Count, _sourceDisplay);

        _summaryMessage = Import3DModelBatch.FormatSummaryMessage(result, _sourceDisplay);
        _summaryType = Import3DModelBatch.SummaryMessageType(result.Succeeded, result.Failures.Count);
        _showSummary = true;
        _pendingSources = [];
    }

    private void ResetTransientUi()
    {
        _showNoProject = false;
        _showPathModal = false;
        _showOverwrite = false;
        _showSummary = false;
        _pendingSources = [];
        _conflictCount = 0;
        _summaryMessage = string.Empty;
        _summaryType = MessageType.Info;
    }
}
