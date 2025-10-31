using System.Numerics;
using ImGuiNET;
using Editor.Managers;
using Editor.Panels;
using Editor.UI;
using Engine;
using Serilog;

namespace Editor.Windows;

/// <summary>
/// Startup window displaying recent projects with quick access options.
/// Shown automatically on editor launch for streamlined workflow.
/// </summary>
public class RecentProjectsWindow
{
    private static readonly ILogger Logger = Log.ForContext<RecentProjectsWindow>();
        
    private bool _isOpen = true;
    private readonly EditorPreferences _editorPreferences;
    private readonly IProjectManager _projectManager;
    private readonly ContentBrowserPanel _contentBrowserPanel;
    private string? _projectToRemove;
    

    public RecentProjectsWindow(
        EditorPreferences editorPreferences,
        IProjectManager projectManager,
        ContentBrowserPanel contentBrowserPanel)
    {
        _editorPreferences = editorPreferences;
        _projectManager = projectManager;
        _contentBrowserPanel = contentBrowserPanel;
    }

    public void OnImGuiRender()
    {
        if (!_isOpen)
            return;

        ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);
            
        // Center window on first appearance
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(
            new Vector2(viewport.Pos.X + viewport.Size.X * 0.5f, viewport.Pos.Y + viewport.Size.Y * 0.5f),
            ImGuiCond.Appearing,
            new Vector2(0.5f, 0.5f)
        );

        var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;

        if (ImGui.Begin("Recent Projects", ref _isOpen, windowFlags))
        {
            DrawRecentProjects();
            ImGui.Separator();
            DrawQuickActions();
        }
        ImGui.End();

        // Handle deferred project removal (can't remove during iteration)
        if (_projectToRemove != null)
        {
            _editorPreferences.RemoveRecentProject(_projectToRemove);
            _projectToRemove = null;
        }
    }

    private void DrawRecentProjects()
    {
        var recentProjects = _editorPreferences.GetRecentProjects();

        if (recentProjects.Count == 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.WarningColor);
            ImGui.TextWrapped("No recent projects found. Create a new project or open an existing one to get started.");
            ImGui.PopStyleColor();
            return;
        }

        ImGui.Text("Recent Projects:");
        ImGui.Spacing();

        // Calculate available height for the list (leaving space for separator and quick actions)
        var availableHeight = ImGui.GetContentRegionAvail().Y - 140; // Reserve space for separator + quick actions
            
        // Begin child region for scrollable list
        if (ImGui.BeginChild("ProjectsList", new Vector2(0, availableHeight), ImGuiChildFlags.Border))
        {
            for (var i = 0; i < recentProjects.Count; i++)
            {
                var project = recentProjects[i];
                DrawProjectItem(project, i);
            }
        }
        ImGui.EndChild();
    }

    private void DrawProjectItem(RecentProject project, int index)
    {
        var projectExists = Directory.Exists(project.Path);
            
        ImGui.PushID(index);
            
        // Project card background
        var cursorPos = ImGui.GetCursorScreenPos();
        var cardSize = new Vector2(ImGui.GetContentRegionAvail().X, 70);
        var drawList = ImGui.GetWindowDrawList();

        var bgColor = ImGui.IsMouseHoveringRect(cursorPos, cursorPos + cardSize)
            ? ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.4f))
            : ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.3f));

        drawList.AddRectFilled(cursorPos, cursorPos + cardSize, bgColor, 4.0f);

        ImGui.BeginGroup();
        ImGui.Spacing();
        ImGui.Indent(10);

        // Project name
        if (!projectExists)
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
            
        ImGui.Text(project.Name);
            
        if (!projectExists)
            ImGui.PopStyleColor();

        // Project path
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
        ImGui.TextWrapped(project.Path);
        ImGui.PopStyleColor();

        // Last opened timestamp
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        var timeAgo = GetTimeAgoString(project.LastOpened);
        ImGui.Text($"Last opened: {timeAgo}");
        ImGui.PopStyleColor();

        ImGui.Unindent(10);
        ImGui.EndGroup();

        // Handle click to open project
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            OpenProject(project);
        }

        // Context menu
        if (ImGui.BeginPopupContextItem($"ProjectContext_{index}"))
        {
            if (ImGui.MenuItem("Open"))
            {
                OpenProject(project);
            }

            if (ImGui.MenuItem("Show in Explorer"))
            {
                ShowInFileExplorer(project.Path);
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Remove from list"))
            {
                _projectToRemove = project.Path;
            }

            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.PopID();
    }

    private void OpenProject(RecentProject project)
    {
        if (!Directory.Exists(project.Path))
        {
            Logger.Warning("Project directory not found: {Path}", project.Path);
            _projectToRemove = project.Path;
            return;
        }

        if (_projectManager.TryOpenProject(project.Path, out var error))
        {
            _contentBrowserPanel.SetRootDirectory(AssetsManager.AssetsPath);
            _isOpen = false;
            Logger.Information("Opened project: {Name}", project.Name);
        }
        else
        {
            Logger.Error("Failed to open project {Path}: {Error}", project.Path, error);
        }
    }

    private void DrawQuickActions()
    {
        ImGui.Text("Quick Actions:");
        ImGui.Spacing();

        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;

        if (ImGui.Button("New Project", new Vector2(buttonWidth, 40)))
        {
            _isOpen = false;
            // ProjectUI will handle showing the new project popup
        }

        ImGui.SameLine();

        if (ImGui.Button("Open Project", new Vector2(buttonWidth, 40)))
        {
            _isOpen = false;
            // ProjectUI will handle showing the open project popup
        }

        ImGui.Spacing();

        if (ImGui.Button("Continue Without Project", new Vector2(-1, 30)))
        {
            _isOpen = false;
        }
    }

    private static string GetTimeAgoString(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;

        switch (timeSpan.TotalMinutes)
        {
            case < 1:
                return "just now";
            case < 60:
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
        }

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
            
        return timeSpan.TotalDays switch
        {
            < 30 => $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago",
            < 365 => $"{(int)(timeSpan.TotalDays / 30)} month{(timeSpan.TotalDays / 30 >= 2 ? "s" : "")} ago",
            _ => timestamp.ToString("yyyy-MM-dd")
        };
    }

    private static void ShowInFileExplorer(string path)
    {
        try
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start("open", path);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                System.Diagnostics.Process.Start("xdg-open", path);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open file explorer for path: {Path}", path);
        }
    }

    /// <summary>
    /// Show the window again.
    /// </summary>
    public void Show()
    {
        _isOpen = true;
    }
}