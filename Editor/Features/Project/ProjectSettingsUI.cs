using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;
using ImGuiNET;

namespace Editor.Features.Project;

public class ProjectSettingsUI(IProjectContext projectContext)
{
    private const float LabelWidth = 140f;
    private const float IntFieldWidth = 120f;
    private const float TextFieldWidth = 320f;

    private bool _open;
    private GameConfiguration? _gameConfig;
    private string _errorMessage = string.Empty;
    private string? _loadedForRoot;

    public void Show()
    {
        _open = true;
        Reload();
    }

    public void Render()
    {
        if (_open && projectContext.Root is null)
        {
            _open = false;
            _gameConfig = null;
            return;
        }

        if (!_open)
            return;

        if (projectContext.Root is not null && !string.Equals(projectContext.Root, _loadedForRoot, StringComparison.Ordinal))
            Reload();

        ImGui.SetNextWindowSizeConstraints(new Vector2(480f, 0f), new Vector2(float.MaxValue, float.MaxValue));

        if (!ModalDrawer.BeginCenteredModal(
                "Project Settings",
                ref _open,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings))
            return;

        if (!string.IsNullOrEmpty(_errorMessage) || _gameConfig is null || projectContext.Root is null)
        {
            TextDrawer.DrawColoredText(
                string.IsNullOrEmpty(_errorMessage) ? "No project is currently loaded." : _errorMessage,
                EditorUIConstants.ErrorColor);
            ModalDrawer.EndModal();
            return;
        }

        ImGui.SeparatorText("Game");

        DrawLabel("Game Title");
        ImGui.SetNextItemWidth(TextFieldWidth);
        var title = _gameConfig.GameTitle;
        if (ImGui.InputText("##gameTitle", ref title, EditorUIConstants.MaxNameLength))
        {
            _gameConfig.GameTitle = title;
            Save();
        }

        DrawLabel("Default Scene");
        LayoutDrawer.DrawComboBox(
            "##defaultScene",
            StartupScenePathForCombo(),
            EnumerateScenes(),
            selected =>
            {
                _gameConfig.StartupScenePath = selected;
                Save();
            },
            width: TextFieldWidth);

        ImGui.SeparatorText("Window");

        DrawLabel("Window Width");
        ImGui.SetNextItemWidth(IntFieldWidth);
        var width = _gameConfig.WindowWidth;
        if (ImGui.InputInt("##windowWidth", ref width))
        {
            _gameConfig.WindowWidth = System.Math.Max(1, width);
            Save();
        }

        DrawLabel("Window Height");
        ImGui.SetNextItemWidth(IntFieldWidth);
        var height = _gameConfig.WindowHeight;
        if (ImGui.InputInt("##windowHeight", ref height))
        {
            _gameConfig.WindowHeight = System.Math.Max(1, height);
            Save();
        }

        DrawLabel("");
        var fullscreen = _gameConfig.Fullscreen;
        if (ImGui.Checkbox("Fullscreen", ref fullscreen))
        {
            _gameConfig.Fullscreen = fullscreen;
            Save();
        }

        DrawLabel("Target Frame Rate");
        ImGui.SetNextItemWidth(IntFieldWidth);
        var fps = _gameConfig.TargetFrameRate;
        if (ImGui.InputInt("##targetFrameRate", ref fps))
        {
            _gameConfig.TargetFrameRate = System.Math.Max(1, fps);
            Save();
        }

        ModalDrawer.EndModal();
    }

    private static void DrawLabel(string label)
    {
        ImGui.AlignTextToFramePadding();
        if (string.IsNullOrEmpty(label))
            ImGui.Dummy(new Vector2(LabelWidth - ImGui.GetStyle().ItemSpacing.X, 0f));
        else
            ImGui.TextUnformatted(label);
        ImGui.SameLine(LabelWidth);
    }

    private void Save()
    {
        if (projectContext.Root is null || _gameConfig is null)
            return;

        GameConfiguration.Save(GameConfiguration.PathFor(projectContext.Root), _gameConfig);
    }

    private void Reload()
    {
        _errorMessage = string.Empty;
        _gameConfig = null;
        _loadedForRoot = projectContext.Root;

        if (projectContext.Root is null)
        {
            _errorMessage = "No project is currently loaded.";
            return;
        }

        var path = GameConfiguration.PathFor(projectContext.Root);
        if (!File.Exists(path))
        {
            var folder = new DirectoryInfo(projectContext.Root).Name;
            var startup = GameConfiguration.FirstSceneRelativePath(projectContext.Root)
                          ?? $"assets/scenes/{folder}.scene";
            GameConfiguration.Save(path, GameConfiguration.ForNewProject(folder, startup));
        }

        if (!GameConfiguration.TryLoad(path, out var config, out var error))
        {
            _errorMessage = error ?? "Failed to load game configuration.";
            return;
        }

        var canonical = GameConfiguration.CanonicalStartupScenePath(projectContext.Root, config.StartupScenePath);
        if (canonical is not null && !string.Equals(canonical, config.StartupScenePath, StringComparison.Ordinal))
        {
            config.StartupScenePath = canonical;
            GameConfiguration.Save(path, config);
        }

        _gameConfig = config;
    }

    private string StartupScenePathForCombo()
    {
        if (_gameConfig is null || projectContext.Root is null)
            return string.Empty;

        var current = _gameConfig.StartupScenePath;
        var match = EnumerateScenes()
            .FirstOrDefault(path => string.Equals(path, current, StringComparison.OrdinalIgnoreCase));
        return match ?? current;
    }

    private string[] EnumerateScenes()
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
}
