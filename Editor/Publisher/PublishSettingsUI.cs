using System.Diagnostics;
using System.Numerics;
using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;
using ImGuiNET;
using Serilog;

namespace Editor.Publisher;

public class PublishSettingsUI(
    IGamePublisher gamePublisher,
    IProjectContext projectContext)
{
    private static readonly ILogger Logger = Log.ForContext<PublishSettingsUI>();

    private const float LabelWidth = 130f;
    
    private static readonly Vector2 ExportProgressModalSize = new(600f, 500f);

    private bool _showExportModal;
    private string _selectedPlatform = PlatformDetection.DetectCurrentPlatform();
    private string _outputPath = "Builds";
    private string _configuration = "Release";
    private string _errorMessage = string.Empty;

    private PublishProgress? _publishProgress;
    private CancellationTokenSource? _publishCts;

    private static readonly string[] Configurations = ["Release", "Debug"];
    private static readonly string[] PlatformDisplayNames =
        PlatformDetection.SupportedRuntimeIdentifiers
            .Select(PlatformDetection.GetPlatformDisplayName)
            .ToArray();

    public void ShowExportModal()
    {
        _showExportModal = true;
        _selectedPlatform = PlatformDetection.DetectCurrentPlatform();
        _outputPath = projectContext.Root is not null
            ? Path.Combine(projectContext.Root, "Builds", new DirectoryInfo(projectContext.Root).Name)
            : "Builds";
        _errorMessage = string.Empty;

        if (projectContext.Root is null)
            _errorMessage = "No project is currently loaded.";
    }

    public void Render()
    {
        RenderExportModal();
        RenderExportProgressModal();
    }

    private void RenderExportModal()
    {
        if (!_showExportModal)
            return;

        if (ModalDrawer.BeginCenteredModal(
                "Export",
                ref _showExportModal,
                ModalDrawer.FormModalFlags))
        {
            RenderBuildFields();

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                LayoutDrawer.DrawSeparatorWithSpacing();
                TextDrawer.DrawErrorText(_errorMessage);
            }

            if (ExportPathAlreadyExists())
            {
                LayoutDrawer.DrawSeparatorWithSpacing();
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(
                    "A build already exists at this export path. Exporting will delete and replace everything in that folder.");
                ImGui.PopStyleColor();
            }

            LayoutDrawer.DrawSeparatorWithSpacing();
            if (ButtonDrawer.DrawCenteredModalButton("Export", disabled: projectContext.Root is null))
                _ = StartExport();

            ModalDrawer.EndModal();
        }
    }

    private bool ExportPathAlreadyExists()
    {
        if (projectContext.Root is null)
            return false;
        return Directory.Exists(ResolveOutputPath(projectContext.Root));
    }

    private void RenderBuildFields()
    {
        LayoutDrawer.DrawFormLabel("Target Platform");
        var platformDisplay = PlatformDetection.GetPlatformDisplayName(_selectedPlatform);
        LayoutDrawer.DrawComboBox(
            "##platform",
            platformDisplay,
            PlatformDisplayNames,
            selected =>
            {
                for (var i = 0; i < PlatformDisplayNames.Length; i++)
                {
                    if (PlatformDisplayNames[i] == selected)
                    {
                        _selectedPlatform = PlatformDetection.SupportedRuntimeIdentifiers[i];
                        break;
                    }
                }
            },
            width: ImGui.GetContentRegionAvail().X);

        ImGui.Spacing();
        LayoutDrawer.DrawFormLabel("Export Path");
        LayoutDrawer.DrawPathField("exportPath", ref _outputPath, () =>
        {
            var initial = !string.IsNullOrWhiteSpace(_outputPath) && Directory.Exists(_outputPath)
                ? _outputPath
                : projectContext.Root ?? Environment.CurrentDirectory;
            var picked = FolderPicker.PickFolder("Select Export Output Folder", initial);
            if (!string.IsNullOrEmpty(picked))
                _outputPath = picked;
        });

        ImGui.Spacing();
        LayoutDrawer.DrawFormLabel("Configuration");
        LayoutDrawer.DrawComboBox(
            "##configuration",
            _configuration,
            Configurations,
            selected => _configuration = selected,
            width: ImGui.GetContentRegionAvail().X);
    }

    private void RenderExportProgressModal()
    {
        if (_publishProgress == null)
            return;

        ImGui.SetNextWindowSize(ExportProgressModalSize, ImGuiCond.Appearing);

        var title = _publishProgress.HasError ? "Export Failed"
            : _publishProgress.IsComplete ? "Export Complete"
            : "Exporting...";

        var isOpen = true;
        // Visible title changes with status; ### id stays stable so ImGui keeps the same popup.
        if (ModalDrawer.BeginCenteredModal($"{title}###ExportProgressModal", ref isOpen,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar,
                minWidth: 0f))
        {
            ImGui.Spacing();

            if (_publishProgress.HasError && !string.IsNullOrEmpty(_publishProgress.ErrorMessage))
            {
                TextDrawer.DrawErrorText(_publishProgress.ErrorMessage);
                ImGui.Spacing();
            }
            else
            {
                ImGui.TextWrapped(_publishProgress.CurrentStep);
                ImGui.Spacing();
            }

            var barColor = PublishProgress.ProgressBarColor(_publishProgress.HasError, _publishProgress.IsComplete);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
            ImGui.ProgressBar(_publishProgress.Progress, new Vector2(-1, 0));
            ImGui.PopStyleColor();
            ImGui.Spacing();

            LayoutDrawer.DrawSeparatorWithSpacing();
            RenderBuildOutput(_publishProgress.BuildOutput, !_publishProgress.IsComplete && !_publishProgress.HasError);
            LayoutDrawer.DrawSeparatorWithSpacing();
            RenderProgressButtons(_publishProgress.IsComplete, _publishProgress.HasError);
            ModalDrawer.EndModal();
        }

        if (!isOpen)
            ClearExportProgress();
    }

    private static void RenderBuildOutput(IEnumerable<string> lines, bool autoScroll)
    {
        ImGui.Text("Build Output:");
        ImGui.BeginChild("BuildOutput", new Vector2(0, 250), ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
        foreach (var line in lines)
        {
            if (line.StartsWith("ERROR:", StringComparison.Ordinal)
                || line.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(line);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.TextWrapped(line);
            }
        }
        if (autoScroll)
            ImGui.SetScrollY(ImGui.GetScrollMaxY());
        ImGui.EndChild();
    }

    private void RenderProgressButtons(bool isComplete, bool hasError)
    {
        if (isComplete && !hasError)
        {
            var openLabel = OpenFolderLabel();
            var openWidth = System.Math.Max(
                EditorUIConstants.StandardButtonWidth,
                ImGui.CalcTextSize(openLabel).X + 24f);
            var spacing = ImGui.GetStyle().ItemSpacing.X;
            ButtonDrawer.BeginCenteredModalButtons(
                openWidth + spacing + EditorUIConstants.StandardButtonWidth);

            ButtonDrawer.DrawModalButton(openLabel, OpenOutputFolder, width: openWidth);
            ImGui.SameLine();
            ButtonDrawer.DrawModalButton("Close", ClearExportProgress);
        }
        else if (hasError)
        {
            ButtonDrawer.BeginCenteredModalButtons(EditorUIConstants.StandardButtonWidth);
            ButtonDrawer.DrawColoredButton("Close", MessageType.Error, ClearExportProgress);
        }
        else
        {
            ButtonDrawer.DrawCenteredModalButton("Cancel", () => _publishCts?.Cancel());
        }
    }

    private static string OpenFolderLabel() =>
        OperatingSystem.IsMacOS() ? "Open in Finder"
        : OperatingSystem.IsWindows() ? "Open in Explorer"
        : "Open Folder";

    private void OpenOutputFolder()
    {
        var path = _publishProgress?.OutputPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start("explorer.exe", path);
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", path);
            else
                Process.Start("xdg-open", path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open export folder: {Path}", path);
        }
    }

    private void ClearExportProgress()
    {
        _publishProgress = null;
        _publishCts?.Dispose();
        _publishCts = null;
    }

    private async Task StartExport()
    {
        if (projectContext.Root is null)
        {
            _errorMessage = "No project is currently loaded.";
            return;
        }

        if (!GameConfiguration.TryLoad(GameConfiguration.PathFor(projectContext.Root), out var gameConfig, out var error)
            || gameConfig is null)
        {
            _errorMessage = error ?? "Game configuration could not be loaded.";
            return;
        }

        var outputPath = ResolveOutputPath(projectContext.Root);
        // Ensure parent exists up front so finalize is not the first place Builds/ is created.
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? outputPath);

        _showExportModal = false;
        _errorMessage = string.Empty;

        var settings = new PublishSettings
        {
            OutputPath = outputPath,
            RuntimeIdentifier = _selectedPlatform,
            Configuration = _configuration,
        };

        _publishProgress = new PublishProgress();
        _publishCts = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(async () =>
                await gamePublisher.PublishAsync(settings, gameConfig, _publishProgress, _publishCts.Token));

            if (result.Success)
                _publishProgress.SetSucceeded(result.OutputPath ?? outputPath);
            else
                _publishProgress.SetFailed(result.ErrorMessage ?? "Export failed.");
        }
        catch (Exception ex)
        {
            _publishProgress.SetFailed($"Unexpected error: {ex.Message}");
        }
    }

    private string ResolveOutputPath(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(_outputPath))
            return Path.Combine(projectDirectory, "Builds", new DirectoryInfo(projectDirectory).Name);
        if (Path.IsPathRooted(_outputPath))
            return _outputPath;
        return Path.Combine(projectDirectory, _outputPath);
    }
}
