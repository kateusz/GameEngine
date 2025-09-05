using System.Numerics;
using ImGuiNET;

namespace Editor.Components;

public class ProjectController
{
    private bool _showNewProjectPopup = false;
    private string _newProjectName = string.Empty;
    private string _newProjectError = string.Empty;
    private string? _currentProjectDirectory = null;
    private bool _showOpenProjectPopup = false;
    private string _openProjectPath = string.Empty;

    public string? CurrentProjectDirectory => _currentProjectDirectory;

    public void Initialize()
    {
        _showOpenProjectPopup = true;
    }

    public void ShowNewProjectDialog()
    {
        _showNewProjectPopup = true;
    }

    public void ShowOpenProjectDialog()
    {
        _showOpenProjectPopup = true;
    }

    public void RenderProjectDialogs()
    {
        RenderNewProjectPopup();
        RenderOpenProjectPopup();
    }

    private void RenderNewProjectPopup()
    {
        if (_showNewProjectPopup)
        {
            ImGui.OpenPopup("New Project");
        }
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("New Project", ref _showNewProjectPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Project Name:");
            ImGui.InputText("##ProjectName", ref _newProjectName, 100);
            ImGui.Separator();
            bool isValid = !string.IsNullOrWhiteSpace(_newProjectName) && System.Text.RegularExpressions.Regex.IsMatch(_newProjectName, @"^[a-zA-Z0-9_\- ]+$");
            if (!isValid && !string.IsNullOrEmpty(_newProjectName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped("Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }
            if (!string.IsNullOrEmpty(_newProjectError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(_newProjectError);
                ImGui.PopStyleColor();
            }
            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Create", new Vector2(120, 0)))
            {
                var result = CreateNewProject(_newProjectName.Trim());
                if (result)
                {
                    _showNewProjectPopup = false;
                    _newProjectName = string.Empty;
                    _newProjectError = string.Empty;
                }
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showNewProjectPopup = false;
                _newProjectName = string.Empty;
                _newProjectError = string.Empty;
            }
            ImGui.EndPopup();
        }
    }

    private void RenderOpenProjectPopup()
    {
        if (_showOpenProjectPopup)
        {
            ImGui.OpenPopup("Open Project");
        }
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("Open Project", ref _showOpenProjectPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter project name:");
            ImGui.InputText("##OpenProjectName", ref _openProjectPath, 100);
            ImGui.Separator();
            bool isValid = !string.IsNullOrWhiteSpace(_openProjectPath) && Directory.Exists(Path.Combine(Environment.CurrentDirectory, _openProjectPath));
            if (!isValid && !string.IsNullOrEmpty(_openProjectPath))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped("Project directory does not exist.");
                ImGui.PopStyleColor();
            }
            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Open", new Vector2(120, 0)))
            {
                var projectDir = Path.Combine(Environment.CurrentDirectory, _openProjectPath.Trim());
                OpenProject(projectDir);
                _showOpenProjectPopup = false;
                _openProjectPath = string.Empty;
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showOpenProjectPopup = false;
                _openProjectPath = string.Empty;
            }
            ImGui.EndPopup();
        }
    }

    private bool CreateNewProject(string projectName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                _newProjectError = "Project name cannot be empty.";
                return false;
            }
        
            var projectDir = Path.Combine(Environment.CurrentDirectory, projectName);
            if (Directory.Exists(projectDir))
            {
                _newProjectError = "A directory with this name already exists.";
                return false;
            }
        
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scenes"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "textures"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "scripts"));
            Directory.CreateDirectory(Path.Combine(projectDir, "assets", "prefabs"));
        
            _currentProjectDirectory = projectDir;
            AssetsManager.SetAssetsPath(Path.Combine(projectDir, "assets"));
            
            OnProjectCreated?.Invoke(projectDir);
            Console.WriteLine($"🆕 Project '{projectName}' created at {projectDir}");
            return true;
        }
        catch (Exception ex)
        {
            _newProjectError = $"Failed to create project: {ex.Message}";
            return false;
        }
    }

    private void OpenProject(string projectDir)
    {
        _currentProjectDirectory = projectDir;
        var assetsDir = Path.Combine(projectDir, "assets");
        if (Directory.Exists(assetsDir))
        {
            AssetsManager.SetAssetsPath(assetsDir);
        }
        else
        {
            AssetsManager.SetAssetsPath(projectDir);
        }
        
        OnProjectOpened?.Invoke(projectDir);
        Console.WriteLine($"📂 Project opened: {projectDir}");
    }

    public void SetScriptsDirectory(string scriptsDir)
    {
        Engine.Scripting.ScriptEngine.Instance.SetScriptsDirectory(scriptsDir);
    }

    public string GetScriptsDirectory()
    {
        string projectRoot = _currentProjectDirectory ?? Environment.CurrentDirectory;
        return Path.Combine(projectRoot, "assets", "scripts");
    }

    // Events
    public event Action<string>? OnProjectCreated;
    public event Action<string>? OnProjectOpened;
}