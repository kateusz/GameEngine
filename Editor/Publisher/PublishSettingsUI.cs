using System.Numerics;
using Editor.Features.Scene;
using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Platform;
using Engine.Project;
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
    private GameConfiguration? _gameConfig;

    private PublishProgress? _publishProgress;
    private CancellationTokenSource? _publishCts;

    private static readonly string[] Configurations = ["Release", "Debug"];

    public void ShowPublishModal()
    {
        _showPublishModal = true;
        _selectedPlatform = PlatformDetection.DetectCurrentPlatform();
        _outputPath = "Builds";
        _errorMessage = string.Empty;
        _gameConfig = null;

        if (projectContext.Root is null)
        {
            _errorMessage = "No project is currently loaded.";
            return;
        }

        var path = GameConfiguration.PathFor(projectContext.Root);
        if (!File.Exists(path))
        {
            var folder = new DirectoryInfo(projectContext.Root).Name;
            GameConfiguration.Save(path, GameConfiguration.ForNewProject(folder, ResolveStartupSceneFallback(folder)));
        }

        if (!GameConfiguration.TryLoad(path, out var config, out var error))
        {
            _errorMessage = error;
            return;
        }

        _gameConfig = config;
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

        ImGui.SetNextWindowSize(EditorUIConstants.PublishSettingsModalSize, ImGuiCond.Appearing);

        if (ModalDrawer.BeginCenteredModal("Publish Game Settings", ref _showPublishModal, ImGuiWindowFlags.NoResize))
        {
            ImGui.Spacing();
            RenderProductFields();
            LayoutDrawer.DrawSeparatorWithSpacing();
            RenderBuildFields();
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

    private void RenderProductFields()
    {
        if (_gameConfig is null)
            return;

        const float fieldWidth = 300f;

        ImGui.Text("Game Title:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(fieldWidth);
        var title = _gameConfig.GameTitle;
        if (ImGui.InputText("##gameTitle", ref title, EditorUIConstants.MaxNameLength))
            _gameConfig.GameTitle = title;

        ImGui.Spacing();
        ImGui.Text("Startup Scene:");
        ImGui.SameLine();
        var scenes = EnumerateStartupScenes();
        LayoutDrawer.DrawComboBox(
            "##startupScene",
            _gameConfig.StartupScenePath,
            scenes,
            selected => _gameConfig.StartupScenePath = selected,
            width: fieldWidth);

        ImGui.Spacing();
        ImGui.Text("Window Width:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(fieldWidth);
        var width = _gameConfig.WindowWidth;
        if (ImGui.InputInt("##windowWidth", ref width))
            _gameConfig.WindowWidth = System.Math.Max(1, width);

        ImGui.Spacing();
        ImGui.Text("Window Height:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(fieldWidth);
        var height = _gameConfig.WindowHeight;
        if (ImGui.InputInt("##windowHeight", ref height))
            _gameConfig.WindowHeight = System.Math.Max(1, height);

        ImGui.Spacing();
        var fullscreen = _gameConfig.Fullscreen;
        if (ImGui.Checkbox("Fullscreen", ref fullscreen))
            _gameConfig.Fullscreen = fullscreen;

        ImGui.Spacing();
        ImGui.Text("Target Frame Rate:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(fieldWidth);
        var fps = _gameConfig.TargetFrameRate;
        if (ImGui.InputInt("##targetFrameRate", ref fps))
            _gameConfig.TargetFrameRate = System.Math.Max(1, fps);
    }

    private void RenderBuildFields()
    {
        ImGui.Text("Target Platform:");
        ImGui.SameLine();
        TextDrawer.DrawColoredText(
            PlatformDetection.GetPlatformDisplayName(_selectedPlatform),
            EditorUIConstants.InfoColor);

        ImGui.Spacing();

        ImGui.Text("Output Path:");
        if (OSInfo.IsWindows)
        {
            var locationLabel = projectContext.Root is not null
                ? ResolveOutputPath(projectContext.Root)
                : (string.IsNullOrWhiteSpace(_outputPath) ? "(no folder selected)" : _outputPath);
            TextDrawer.DrawColoredText(locationLabel, EditorUIConstants.InfoColor);

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
                    EditorUIConstants.InfoColor);
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

        if (_gameConfig is null)
        {
            _errorMessage = string.IsNullOrEmpty(_errorMessage)
                ? "Game configuration could not be loaded."
                : _errorMessage;
            return;
        }

        GameConfiguration.Save(GameConfiguration.PathFor(projectContext.Root), _gameConfig);

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

        var gameConfig = _gameConfig;
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

    private string ResolveStartupSceneFallback(string folder)
    {
        var current = sceneManager.GetCurrentScenePath();
        if (!string.IsNullOrEmpty(current) && projectContext.Root is not null)
            return Path.GetRelativePath(projectContext.Root, current).Replace('\\', '/');
        return $"assets/scenes/{folder}.scene";
    }

    private string[] EnumerateStartupScenes()
    {
        var root = projectContext.Root;
        var scenesDir = projectContext.ScenesDir;
        var current = _gameConfig?.StartupScenePath;

        if (root is null || scenesDir is null || !Directory.Exists(scenesDir))
            return string.IsNullOrEmpty(current) ? [] : [current];

        var scenes = Directory.EnumerateFiles(scenesDir, "*.scene", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrEmpty(current)
            && !scenes.Contains(current, StringComparer.OrdinalIgnoreCase))
            scenes.Add(current);

        return scenes.ToArray();
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
