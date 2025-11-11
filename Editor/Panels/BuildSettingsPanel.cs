using System.Numerics;
using Editor.Publisher;
using Editor.UI;
using ImGuiNET;
using Serilog;

namespace Editor.Panels;

/// <summary>
/// ImGui panel for configuring and triggering game builds
/// </summary>
public class BuildSettingsPanel
{
    private static readonly ILogger Logger = Log.ForContext<BuildSettingsPanel>();

    private bool _isOpen = false;
    private BuildSettings _settings;
    private readonly GamePublisher _publisher;
    private string _settingsPath;

    // UI state
    private bool _isBuilding = false;
    private string _buildStatus = "";
    private List<string> _buildProgressLog = new();
    private readonly object _buildProgressLogLock = new();
    private BuildReport? _lastBuildReport;
    private bool _showBuildReport = false;

    // Scene selection
    private string[] _availableScenes = Array.Empty<string>();
    private int _selectedSceneIndex = 0;

    public BuildSettingsPanel(GamePublisher publisher)
    {
        _publisher = publisher;
        _settings = new BuildSettings();
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "BuildSettings.json");

        // Subscribe to publisher events
        _publisher.OnBuildProgress += OnBuildProgress;
        _publisher.OnBuildComplete += OnBuildComplete;

        // Load settings
        LoadSettings();
        RefreshAvailableScenes();
    }

    public bool IsOpen
    {
        get => _isOpen;
        set => _isOpen = value;
    }

    public void Show()
    {
        _isOpen = true;
        RefreshAvailableScenes();
    }

    public void OnImGuiRender()
    {
        if (!_isOpen)
            return;

        ImGui.SetNextWindowSize(new Vector2(600, 700), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Build Settings", ref _isOpen, ImGuiWindowFlags.None))
        {
            RenderBuildConfiguration();
            ImGui.Separator();
            RenderPlatformSettings();
            ImGui.Separator();
            RenderSceneSettings();
            ImGui.Separator();
            RenderScriptSettings();
            ImGui.Separator();
            RenderWindowSettings();
            ImGui.Separator();
            RenderOutputSettings();
            ImGui.Separator();
            RenderBuildActions();

            // Show build progress
            if (_isBuilding)
            {
                ImGui.Separator();
                RenderBuildProgress();
            }
        }

        ImGui.End();

        // Render build report window separately
        if (_showBuildReport && _lastBuildReport != null)
        {
            RenderBuildReportWindow();
        }
    }

    private void RenderBuildConfiguration()
    {
        ImGui.Text("Build Configuration");
        ImGui.Indent();

        int configIndex = (int)_settings.Configuration;
        if (ImGui.RadioButton("Debug", ref configIndex, (int)BuildConfiguration.Debug))
            _settings.Configuration = BuildConfiguration.Debug;

        ImGui.SameLine();
        if (ImGui.RadioButton("Release", ref configIndex, (int)BuildConfiguration.Release))
            _settings.Configuration = BuildConfiguration.Release;

        ImGui.Unindent();
    }

    private void RenderPlatformSettings()
    {
        ImGui.Text("Target Platform");
        ImGui.Indent();

        var platforms = Enum.GetValues<TargetPlatform>();
        int currentPlatform = (int)_settings.Platform;

        if (ImGui.BeginCombo("Platform", _settings.Platform.ToString()))
        {
            foreach (var platform in platforms)
            {
                bool isSelected = _settings.Platform == platform;
                if (ImGui.Selectable(GetPlatformDisplayName(platform), isSelected))
                {
                    _settings.Platform = platform;
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), $"Runtime ID: {_settings.GetRuntimeIdentifier()}");

        ImGui.Unindent();
    }

    private void RenderSceneSettings()
    {
        ImGui.Text("Startup Scene");
        ImGui.Indent();

        if (_availableScenes.Length == 0)
        {
            ImGui.TextColored(EditorUIConstants.WarningColor, "No scenes found in assets/scenes/");
            if (ImGui.Button("Refresh Scenes"))
            {
                RefreshAvailableScenes();
            }
        }
        else
        {
            if (ImGui.BeginCombo("Scene", _settings.StartupScene))
            {
                for (int i = 0; i < _availableScenes.Length; i++)
                {
                    bool isSelected = _settings.StartupScene == _availableScenes[i];
                    if (ImGui.Selectable(_availableScenes[i], isSelected))
                    {
                        _settings.StartupScene = _availableScenes[i];
                        _selectedSceneIndex = i;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (ImGui.Button("Refresh"))
            {
                RefreshAvailableScenes();
            }
        }

        ImGui.Unindent();
    }

    private void RenderScriptSettings()
    {
        ImGui.Text("Script Settings");
        ImGui.Indent();

        bool precompileScripts = _settings.PrecompileScripts;
        if (ImGui.Checkbox("Pre-compile Scripts", ref precompileScripts))
        {
            _settings.PrecompileScripts = precompileScripts;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Compile scripts to GameScripts.dll at build time\nRecommended for distribution");
        }

        ImGui.BeginDisabled(!_settings.PrecompileScripts);
        bool includeRoslyn = _settings.IncludeRoslyn;
        if (ImGui.Checkbox("Include Roslyn (Enable Modding)", ref includeRoslyn))
        {
            _settings.IncludeRoslyn = includeRoslyn;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Include script compiler in build for runtime modding\nIncreases build size by ~20MB");
        }
        ImGui.EndDisabled();

        ImGui.Unindent();
    }

    private void RenderWindowSettings()
    {
        ImGui.Text("Window Settings");
        ImGui.Indent();

        var gameName = _settings.GameName;
        if (ImGui.InputText("Game Name", ref gameName, 256))
        {
            _settings.GameName = gameName;
        }

        var windowTitle = _settings.WindowTitle;
        if (ImGui.InputText("Window Title", ref windowTitle, 256))
        {
            _settings.WindowTitle = windowTitle;
        }

        int width = _settings.WindowWidth;
        if (ImGui.InputInt("Width", ref width))
        {
            _settings.WindowWidth = Math.Max(640, width);
        }

        int height = _settings.WindowHeight;
        if (ImGui.InputInt("Height", ref height))
        {
            _settings.WindowHeight = Math.Max(480, height);
        }

        bool fullscreen = _settings.Fullscreen;
        if (ImGui.Checkbox("Fullscreen", ref fullscreen))
        {
            _settings.Fullscreen = fullscreen;
        }

        bool vsync = _settings.VSync;
        if (ImGui.Checkbox("VSync", ref vsync))
        {
            _settings.VSync = vsync;
        }

        ImGui.Unindent();
    }

    private void RenderOutputSettings()
    {
        ImGui.Text("Output Settings");
        ImGui.Indent();

        var outputDir = _settings.OutputDirectory;
        if (ImGui.InputText("Output Directory", ref outputDir, 512))
        {
            _settings.OutputDirectory = outputDir;
        }

        if (ImGui.Button("Open Output Folder"))
        {
            OpenOutputFolder();
        }

        ImGui.Unindent();
    }

    private void RenderBuildActions()
    {
        ImGui.Separator();

        var buttonWidth = ImGui.GetContentRegionAvail().X;

        ImGui.BeginDisabled(_isBuilding);

        if (ImGui.Button("Build Game", new Vector2(buttonWidth, EditorUIConstants.StandardButtonHeight)))
        {
            SaveSettings();
            StartBuild();
        }

        ImGui.EndDisabled();

        ImGui.Spacing();

        if (ImGui.Button("Save Settings", new Vector2(buttonWidth / 2 - 4, EditorUIConstants.StandardButtonHeight)))
        {
            SaveSettings();
        }

        ImGui.SameLine();

        if (ImGui.Button("Load Settings", new Vector2(buttonWidth / 2 - 4, EditorUIConstants.StandardButtonHeight)))
        {
            LoadSettings();
        }
    }

    private void RenderBuildProgress()
    {
        ImGui.Text("Build Progress");
        ImGui.Indent();

        ImGui.TextColored(EditorUIConstants.InfoColor, _buildStatus);

        // Show progress log
        lock (_buildProgressLogLock)
        {
            if (_buildProgressLog.Count > 0)
            {
                ImGui.BeginChild("BuildLog", new Vector2(0, 150), ImGuiChildFlags.Border);
                foreach (var line in _buildProgressLog)
                {
                    ImGui.TextWrapped(line);
                }
                // Auto-scroll to bottom
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1.0f);
                ImGui.EndChild();
            }
        }

        ImGui.Unindent();
    }

    private void RenderBuildReportWindow()
    {
        if (_lastBuildReport == null)
            return;

        ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Build Report", ref _showBuildReport))
        {
            var report = _lastBuildReport;

            // Header
            if (report.Success)
            {
                ImGui.TextColored(EditorUIConstants.SuccessColor, "✓ Build Successful");
            }
            else
            {
                ImGui.TextColored(EditorUIConstants.ErrorColor, "✗ Build Failed");
            }

            ImGui.Separator();

            // Build info
            ImGui.Text($"Duration: {report.BuildDuration.TotalSeconds:F2}s");
            ImGui.Text($"Output: {Path.GetFileName(report.OutputDirectory)}");

            ImGui.Separator();

            // Files
            if (report.Files.Count > 0)
            {
                ImGui.Text("Output Files:");
                ImGui.Indent();
                foreach (var file in report.Files)
                {
                    ImGui.BulletText(file);
                }
                ImGui.Unindent();
            }

            // Errors
            if (report.Errors.Count > 0)
            {
                ImGui.Separator();
                ImGui.TextColored(EditorUIConstants.ErrorColor, $"Errors ({report.Errors.Count}):");
                ImGui.BeginChild("ErrorList", new Vector2(0, 150), ImGuiChildFlags.Border);
                foreach (var error in report.Errors)
                {
                    ImGui.TextWrapped(error);
                }
                ImGui.EndChild();
            }

            ImGui.Separator();

            // Actions
            if (ImGui.Button("Open Output Folder", new Vector2(ImGui.GetContentRegionAvail().X, EditorUIConstants.StandardButtonHeight)))
            {
                OpenOutputFolder();
            }

            if (report.Success && ImGui.Button("Close", new Vector2(ImGui.GetContentRegionAvail().X, EditorUIConstants.StandardButtonHeight)))
            {
                _showBuildReport = false;
            }
        }

        ImGui.End();
    }

    private void RefreshAvailableScenes()
    {
        try
        {
            var scenesPath = Path.Combine(AppContext.BaseDirectory, "assets", "scenes");
            if (Directory.Exists(scenesPath))
            {
                _availableScenes = Directory.GetFiles(scenesPath, "*.scene*")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Select(f => f!)
                    .ToArray();

                // Try to find current selection
                if (!string.IsNullOrEmpty(_settings.StartupScene))
                {
                    for (int i = 0; i < _availableScenes.Length; i++)
                    {
                        if (_availableScenes[i] == _settings.StartupScene)
                        {
                            _selectedSceneIndex = i;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to refresh available scenes");
        }
    }

    private void StartBuild()
    {
        _isBuilding = true;
        _buildStatus = "Starting build...";

        lock (_buildProgressLogLock)
        {
            _buildProgressLog.Clear();
        }

        Task.Run(async () =>
        {
            try
            {
                var report = await _publisher.PublishAsync(_settings);
                // OnBuildComplete will be called by the publisher
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Build exception");
                _buildStatus = "Build failed with exception";
                _isBuilding = false;
            }
        });
    }

    private void OnBuildProgress(string message)
    {
        _buildStatus = message;

        lock (_buildProgressLogLock)
        {
            _buildProgressLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        Logger.Information("Build: {Message}", message);
    }

    private void OnBuildComplete(BuildReport report)
    {
        _isBuilding = false;
        _lastBuildReport = report;
        _showBuildReport = true;

        if (report.Success)
        {
            _buildStatus = "Build completed successfully!";
            Logger.Information("Build completed successfully");
        }
        else
        {
            _buildStatus = "Build failed - see report for details";
            Logger.Error("Build failed");
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                _settings = BuildSettings.Load(_settingsPath);
                Logger.Information("Build settings loaded from: {SettingsPath}", _settingsPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load build settings");
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settings.Save(_settingsPath);
            Logger.Information("Build settings saved to: {SettingsPath}", _settingsPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save build settings");
        }
    }

    private void OpenOutputFolder()
    {
        try
        {
            var outputPath = _settings.OutputDirectory;
            if (!Path.IsPathRooted(outputPath))
            {
                outputPath = Path.Combine(AppContext.BaseDirectory, "..", outputPath);
            }
            outputPath = Path.GetFullPath(outputPath);

            if (Directory.Exists(outputPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Logger.Warning("Output directory does not exist: {OutputPath}", outputPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open output folder");
        }
    }

    private string GetPlatformDisplayName(TargetPlatform platform)
    {
        return platform switch
        {
            TargetPlatform.Win64 => "Windows x64",
            TargetPlatform.Win86 => "Windows x86",
            TargetPlatform.WinARM64 => "Windows ARM64",
            TargetPlatform.Linux64 => "Linux x64",
            TargetPlatform.LinuxARM64 => "Linux ARM64",
            TargetPlatform.MacOS64 => "macOS Intel (x64)",
            TargetPlatform.MacOSARM64 => "macOS Apple Silicon (ARM64)",
            _ => platform.ToString()
        };
    }
}
