using System.Numerics;
using Editor.Features.Scene;
using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Platform;
using ImGuiNET;

namespace Editor.Publisher;

public class PublishSettingsUI(
    IGamePublisher gamePublisher,
    IProjectContext projectContext,
    ISceneManager sceneManager)
{
    private bool _showPublishModal;
    private string _selectedPlatform = PlatformDetection.DetectCurrentPlatform();
    private string _outputPath = "Builds";
    private bool _selfContained = true;
    private bool _singleFile = true;
    private string _configuration = "Release";
    private string _errorMessage = string.Empty;

    private PublishProgress? _publishProgress;
    private CancellationTokenSource? _publishCts;

    private static readonly string[] SupportedPlatforms =
    [
        "win-x64", "win-x86", "win-arm64",
        "osx-x64", "osx-arm64"
    ];

    private static readonly string[] Configurations = ["Release", "Debug"];

    public void ShowPublishModal()
    {
        _showPublishModal = true;
        _selectedPlatform = PlatformDetection.DetectCurrentPlatform();
        _outputPath = "Builds";
        _errorMessage = string.Empty;
    }

    public void Render()
    {
        RenderPublishSettingsModal();
        RenderPublishProgressModal();
    }

    private void RenderPublishSettingsModal()
    {
        if (!_showPublishModal)
            return;

        ImGui.SetNextWindowSize(EditorUIConstants.PublishSettingsModalSize, ImGuiCond.FirstUseEver);

        if (ModalDrawer.BeginCenteredModal("Publish Game Settings", ref _showPublishModal, ImGuiWindowFlags.NoResize))
        {
            ImGui.Spacing();

            ImGui.Text("Target Platform:");
            ImGui.SameLine();
            LayoutDrawer.DrawComboBox(
                "##platform",
                PlatformDetection.GetPlatformDisplayName(_selectedPlatform),
                SupportedPlatforms.Select(PlatformDetection.GetPlatformDisplayName).ToArray(),
                selectedDisplay =>
                {
                    _selectedPlatform = SupportedPlatforms.First(p =>
                        PlatformDetection.GetPlatformDisplayName(p) == selectedDisplay);
                },
                width: 300
            );

            ImGui.Spacing();

            ImGui.Text("Output Path:");
            if (OSInfo.IsWindows)
            {
                var locationLabel = projectContext.Root is not null
                    ? ResolveOutputPath(projectContext.Root)
                    : (string.IsNullOrWhiteSpace(_outputPath) ? "(no folder selected)" : _outputPath);
                TextDrawer.DrawColoredText(locationLabel, new Vector4(0.7f, 0.7f, 0.7f, 1f));

                ImGui.Spacing();
                if (ImGui.Button("Select Folder..."))
                {
                    var initial = projectContext.Root is not null
                        ? ResolveParentPath(projectContext.Root)
                        : Environment.CurrentDirectory;
                    var picked = FolderPicker.PickFolder("Select Publish Output Folder", initial);
                    if (!string.IsNullOrEmpty(picked))
                        _outputPath = picked;
                }
            }
            else
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(300);
                ImGui.InputText("##outputPath", ref _outputPath, 256);
                if (projectContext.Root is not null)
                {
                    ImGui.Spacing();
                    TextDrawer.DrawColoredText(
                        ResolveOutputPath(projectContext.Root),
                        new Vector4(0.7f, 0.7f, 0.7f, 1f));
                }
            }

            ImGui.Spacing();

            ImGui.Text("Configuration:");
            ImGui.SameLine();
            LayoutDrawer.DrawComboBox(
                "##configuration",
                _configuration,
                Configurations,
                selected => _configuration = selected,
                width: 300
            );

            LayoutDrawer.DrawSeparatorWithSpacing();

            ImGui.Checkbox("Self-Contained (includes .NET runtime)", ref _selfContained);
            ImGui.Checkbox("Single File (package as single executable)", ref _singleFile);

            LayoutDrawer.DrawSeparatorWithSpacing();

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_errorMessage);
                ImGui.PopStyleColor();
                ImGui.Spacing();
            }

            ImGui.Spacing();
            var buttonWidth = 100.0f;
            var availWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX((availWidth - buttonWidth * 2 - ImGui.GetStyle().ItemSpacing.X) / 2);

            if (ButtonDrawer.DrawColoredButton("Publish", MessageType.Success, width: buttonWidth))
                _ = StartPublish();

            ImGui.SameLine();

            if (ButtonDrawer.DrawButton("Cancel", width: buttonWidth, height: EditorUIConstants.StandardButtonHeight))
            {
                _showPublishModal = false;
                _errorMessage = string.Empty;
            }

            ModalDrawer.EndModal();
        }
    }

    private void RenderPublishProgressModal()
    {
        if (_publishProgress == null)
            return;

        ImGui.SetNextWindowSize(EditorUIConstants.PublishProgressModalSize, ImGuiCond.Appearing);

        var title = _publishProgress.HasError ? "Publish Failed"
            : _publishProgress.IsComplete ? "Publish Complete"
            : "Publishing Game...";

        var isOpen = true;
        // Visible title changes with status; ### id stays stable so ImGui keeps the same popup.
        if (ModalDrawer.BeginCenteredModal($"{title}###PublishProgressModal", ref isOpen,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Spacing();

            if (_publishProgress.HasError && !string.IsNullOrEmpty(_publishProgress.ErrorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
                ImGui.TextWrapped(_publishProgress.ErrorMessage);
                ImGui.PopStyleColor();
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
            ClearPublishProgress();
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
        const float buttonWidth = 100.0f;
        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX((availWidth - buttonWidth) / 2);

        if (isComplete || hasError)
        {
            var closeType = hasError ? MessageType.Error : MessageType.Success;
            if (ButtonDrawer.DrawColoredButton("Close", closeType, width: buttonWidth))
                ClearPublishProgress();
        }
        else if (ButtonDrawer.DrawColoredButton("Cancel", MessageType.Warning, width: buttonWidth))
        {
            _publishCts?.Cancel();
        }
    }

    private void ClearPublishProgress()
    {
        _publishProgress = null;
        _publishCts?.Dispose();
        _publishCts = null;
    }

    private async Task StartPublish()
    {
        if (projectContext.Root == null)
        {
            _errorMessage = "No project is currently loaded.";
            return;
        }

        var currentScene = sceneManager.GetCurrentScenePath();
        if (string.IsNullOrEmpty(currentScene))
        {
            _errorMessage = "Please save the current scene before publishing.";
            return;
        }

        var outputPath = ResolveOutputPath(projectContext.Root);
        // Ensure parent exists up front so finalize is not the first place Builds/ is created.
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        _showPublishModal = false;
        _errorMessage = string.Empty;

        var settings = new PublishSettings
        {
            OutputPath = outputPath,
            RuntimeIdentifier = _selectedPlatform,
            SelfContained = _selfContained,
            SingleFile = _singleFile,
            Configuration = _configuration,
        };

        var gameConfig = new GameConfiguration
        {
            GameTitle = new DirectoryInfo(projectContext.Root).Name,
            StartupScenePath = Path.GetRelativePath(projectContext.Root, currentScene)
                .Replace('\\', '/'),
            WindowWidth = 1920,
            WindowHeight = 1080
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
                _publishProgress.SetFailed(result.ErrorMessage ?? "Publish failed.");
        }
        catch (Exception ex)
        {
            _publishProgress.SetFailed($"Unexpected error: {ex.Message}");
        }
    }

    private string ResolveParentPath(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(_outputPath))
            return Path.Combine(projectDirectory, "Builds");
        if (Path.IsPathRooted(_outputPath))
            return _outputPath;
        return Path.Combine(projectDirectory, _outputPath);
    }

    private string ResolveOutputPath(string projectDirectory)
        => Path.Combine(ResolveParentPath(projectDirectory), new DirectoryInfo(projectDirectory).Name);
}
