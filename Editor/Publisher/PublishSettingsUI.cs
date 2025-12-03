using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Publisher;

public class PublishSettingsUI(
    IGamePublisher gamePublisher,
    IProjectManager projectManager,
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

    private static readonly string[] SupportedPlatforms = new[]
    {
        "win-x64", "win-x86", "win-arm64",
        "osx-x64", "osx-arm64",
        "linux-x64", "linux-arm64"
    };

    private static readonly string[] Configurations = new[] { "Release", "Debug" };

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

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 400), ImGuiCond.FirstUseEver);

        if (ModalDrawer.BeginCenteredModal("Publish Game Settings", ref _showPublishModal, ImGuiWindowFlags.NoResize))
        {
            ImGui.Spacing();

            // Platform selection using LayoutDrawer
            ImGui.Text("Target Platform:");
            ImGui.SameLine();
            LayoutDrawer.DrawComboBox(
                "##platform",
                PlatformDetection.GetPlatformDisplayName(_selectedPlatform),
                SupportedPlatforms.Select(PlatformDetection.GetPlatformDisplayName).ToArray(),
                selectedDisplay =>
                {
                    // Find platform by display name
                    _selectedPlatform = SupportedPlatforms.First(p =>
                        PlatformDetection.GetPlatformDisplayName(p) == selectedDisplay);
                },
                width: 300
            );

            ImGui.Spacing();

            // Output path
            ImGui.Text("Output Path:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300);
            ImGui.InputText("##outputPath", ref _outputPath, 256);

            ImGui.Spacing();

            // Configuration using LayoutDrawer
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

            // Options
            ImGui.Checkbox("Self-Contained (includes .NET runtime)", ref _selfContained);
            ImGui.Checkbox("Single File (package as single executable)", ref _singleFile);

            LayoutDrawer.DrawSeparatorWithSpacing();

            // Error message using TextDrawer
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                TextDrawer.DrawErrorText(_errorMessage);
                ImGui.Spacing();
            }

            // Buttons - centered
            ImGui.Spacing();
            var buttonWidth = 100.0f;
            var availWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX((availWidth - buttonWidth * 2 - ImGui.GetStyle().ItemSpacing.X) / 2);

            if (ButtonDrawer.DrawColoredButton("Publish", MessageType.Success, width: buttonWidth))
            {
                StartPublish();
            }

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

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 400), ImGuiCond.FirstUseEver);

        var isOpen = true;
        if (ModalDrawer.BeginCenteredModal("Publishing Game...", ref isOpen, ImGuiWindowFlags.NoResize))
        {
            ImGui.Spacing();

            // Current step
            ImGui.TextWrapped(_publishProgress.CurrentStep);
            ImGui.Spacing();

            // Progress bar
            ImGui.ProgressBar(_publishProgress.Progress, new System.Numerics.Vector2(-1, 0));
            ImGui.Spacing();

            LayoutDrawer.DrawSeparatorWithSpacing();

            // Build output (scrollable)
            ImGui.Text("Build Output:");
            ImGui.BeginChild("BuildOutput", new System.Numerics.Vector2(0, 250), ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);

            foreach (var line in _publishProgress.BuildOutput)
            {
                if (line.StartsWith("ERROR:"))
                {
                    TextDrawer.DrawErrorText(line);
                }
                else
                {
                    ImGui.TextWrapped(line);
                }
            }

            // Auto-scroll to bottom
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();

            LayoutDrawer.DrawSeparatorWithSpacing();

            // Buttons - centered
            var buttonWidth = 100.0f;
            var availWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX((availWidth - buttonWidth) / 2);

            if (_publishProgress.IsComplete || _publishProgress.HasError)
            {
                if (ButtonDrawer.DrawColoredButton("Close", MessageType.Success, width: buttonWidth))
                {
                    _publishProgress = null;
                    _publishCts?.Dispose();
                    _publishCts = null;
                }
            }
            else
            {
                if (ButtonDrawer.DrawColoredButton("Cancel", MessageType.Warning, width: buttonWidth))
                {
                    _publishCts?.Cancel();
                }
            }

            ModalDrawer.EndModal();
        }

        // Handle modal close via X button
        if (!isOpen)
        {
            _publishProgress = null;
            _publishCts?.Dispose();
            _publishCts = null;
        }
    }

    private async void StartPublish()
    {
        if (projectManager.CurrentProjectDirectory == null)
        {
            _errorMessage = "No project is currently loaded.";
            return;
        }

        // Get the current scene path
        var currentScene = sceneManager.GetCurrentScenePath();
        if (string.IsNullOrEmpty(currentScene))
        {
            _errorMessage = "Please save the current scene before publishing.";
            return;
        }

        _showPublishModal = false;
        _errorMessage = string.Empty;

        // Create publish settings
        var settings = new PublishSettings
        {
            OutputPath = string.IsNullOrWhiteSpace(_outputPath)
                ? Path.Combine(projectManager.CurrentProjectDirectory, "Builds")
                : Path.IsPathRooted(_outputPath)
                    ? _outputPath
                    : Path.Combine(projectManager.CurrentProjectDirectory, _outputPath),
            RuntimeIdentifier = _selectedPlatform,
            SelfContained = _selfContained,
            SingleFile = _singleFile,
            Configuration = _configuration
        };

        // Create game configuration
        var gameConfig = new GameConfiguration
        {
            StartupScenePath = Path.GetRelativePath(projectManager.CurrentProjectDirectory, currentScene)
                .Replace('\\', '/'),
            WindowWidth = 1920,
            WindowHeight = 1080,
            Fullscreen = false,
            GameTitle = Path.GetFileName(projectManager.CurrentProjectDirectory) ?? "My Game",
            TargetFrameRate = 60
        };

        // Start publishing in background
        _publishProgress = new PublishProgress();
        _publishCts = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(async () =>
                await gamePublisher.PublishAsync(settings, gameConfig, _publishProgress, _publishCts.Token));

            _publishProgress.IsComplete = true;
            _publishProgress.HasError = !result.Success;

            if (result.Success)
            {
                _publishProgress.Report($"✓ Publish completed successfully!");
                _publishProgress.Report($"Output: {result.OutputPath}");
                _publishProgress.SetProgress(1.0f);
            }
            else
            {
                _publishProgress.Report($"✗ Publish failed: {result.ErrorMessage}");
                _publishProgress.HasError = true;
            }
        }
        catch (Exception ex)
        {
            _publishProgress.Report($"✗ Unexpected error: {ex.Message}");
            _publishProgress.HasError = true;
            _publishProgress.IsComplete = true;
        }
    }
}
