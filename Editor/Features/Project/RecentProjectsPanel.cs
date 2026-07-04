using System.Numerics;
using Editor.Features.Settings;
using Editor.Panels;
using Editor.UI.Drawers;
using Engine.Core;
using ImGuiNET;
using Serilog;

namespace Editor.Features.Project;

public class RecentProjectsPanel(
    IEditorPreferences editorPreferences,
    IProjectManager projectManager,
    NewProjectPopup newProjectPopup) : IEditorPanel
{
    private static readonly ILogger Logger = Log.ForContext<RecentProjectsPanel>();

    private bool _isOpen = true;
    private bool _isLoading;
    private string _loadingProjectName = string.Empty;
    private string? _projectToRemove;
    private string? _pendingOpenPath;
    private float _loadingSpinnerRotation;

    public void Draw()
    {
        if (_pendingOpenPath is { } pendingPath)
            ProcessPendingOpen(pendingPath);

        if (!_isOpen)
            return;

        ImGui.SetNextWindowSize(new Vector2(DisplayConfig.StandardPopupSize.Width, DisplayConfig.StandardPopupSize.Height), ImGuiCond.FirstUseEver);

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(
            new Vector2(viewport.Pos.X + viewport.Size.X * 0.5f, viewport.Pos.Y + viewport.Size.Y * 0.5f),
            ImGuiCond.Appearing,
            new Vector2(0.5f, 0.5f)
        );

        var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;

        if (ImGui.Begin("Recent Projects", ref _isOpen, windowFlags))
        {
            if (_isLoading)
                DrawLoadingOverlay();
            else
            {
                DrawRecentProjects();
                ImGui.Separator();
                DrawQuickActions();
            }
        }
        ImGui.End();

        if (_projectToRemove != null)
        {
            editorPreferences.RemoveRecentProject(_projectToRemove);
            _projectToRemove = null;
        }
    }

    private void DrawRecentProjects()
    {
        var recentProjects = editorPreferences.GetRecentProjects();

        if (recentProjects.Count == 0)
        {
            TextDrawer.DrawWarningText("No recent projects found. Create a new project or open an existing one to get started.");
            return;
        }

        ImGui.Text("Recent Projects:");
        ImGui.Spacing();

        var availableHeight = ImGui.GetContentRegionAvail().Y - 140;

        if (ImGui.BeginChild("ProjectsList", new Vector2(0, availableHeight), ImGuiChildFlags.Border))
        {
            for (var i = 0; i < recentProjects.Count; i++)
                DrawProjectItem(recentProjects[i], i);
        }
        ImGui.EndChild();
    }

    private void DrawProjectItem(RecentProject project, int index)
    {
        var projectExists = Directory.Exists(project.Path);

        ImGui.PushID(index);

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

        if (!projectExists)
            TextDrawer.DrawErrorText(project.Name);
        else
            ImGui.Text(project.Name);

        TextDrawer.DrawColoredText(project.Path, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));

        var timeAgo = GetTimeAgoString(project.LastOpened);
        TextDrawer.DrawColoredText($"Last opened: {timeAgo}", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

        ImGui.Unindent(10);
        ImGui.EndGroup();

        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            QueueOpenProject(project);

        if (ImGui.BeginPopupContextItem($"ProjectContext_{index}"))
        {
            if (ImGui.MenuItem("Open"))
                QueueOpenProject(project);

            if (ImGui.MenuItem("Show in Explorer"))
                ShowInFileExplorer(project.Path);

            ImGui.Separator();

            if (ImGui.MenuItem("Remove from list"))
                _projectToRemove = project.Path;

            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.PopID();
    }

    private void QueueOpenProject(RecentProject project)
    {
        if (!Directory.Exists(project.Path))
        {
            Logger.Warning("Project directory not found: {Path}", project.Path);
            _projectToRemove = project.Path;
            return;
        }

        _pendingOpenPath = project.Path;
        _loadingProjectName = project.Name;
        _loadingSpinnerRotation = 0.0f;
        _isLoading = true;
    }

    private void ProcessPendingOpen(string path)
    {
        _pendingOpenPath = null;

        try
        {
            if (projectManager.TryOpenProject(path, out var error))
            {
                Logger.Information("Opened project from recent list: {Path}", path);
                _isOpen = false;
            }
            else
                Logger.Error("Failed to open project {Path}: {Error}", path, error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void DrawQuickActions()
    {
        ImGui.Text("Quick Actions:");
        ImGui.Spacing();

        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;

        ButtonDrawer.DrawModalButton("New Project", () =>
        {
            newProjectPopup.ShowNewProjectPopup();
            _isOpen = false;
        }, buttonWidth, 20);

        ImGui.SameLine();

        ButtonDrawer.DrawModalButton("Continue Without Project", () =>
        {
            _isOpen = false;
        }, buttonWidth, 20);
    }

    private void DrawLoadingOverlay()
    {
        var windowSize = ImGui.GetWindowSize();
        var windowPos = ImGui.GetWindowPos();

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(
            windowPos,
            new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
            ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f))
        );

        var centerX = windowPos.X + windowSize.X * 0.5f;
        var centerY = windowPos.Y + windowSize.Y * 0.5f;

        _loadingSpinnerRotation += ImGui.GetIO().DeltaTime * 3.0f;

        const float spinnerRadius = 30.0f;
        const int segments = 12;
        const float thickness = 4.0f;

        for (var i = 0; i < segments; i++)
        {
            var angle = (_loadingSpinnerRotation + (i * MathF.PI * 2.0f / segments)) % (MathF.PI * 2.0f);
            var alpha = 1.0f - (i / (float)segments);

            var startAngle = angle;
            var endAngle = angle + (MathF.PI * 2.0f / segments * 0.8f);

            drawList.PathArcTo(
                new Vector2(centerX, centerY),
                spinnerRadius,
                startAngle,
                endAngle,
                10
            );

            drawList.PathStroke(
                ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, alpha)),
                0,
                thickness
            );
        }

        var loadingText = $"Loading {_loadingProjectName}...";
        var textSize = ImGui.CalcTextSize(loadingText);

        drawList.AddText(
            new Vector2(centerX - textSize.X * 0.5f, centerY + spinnerRadius + 20),
            ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
            loadingText
        );
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
                System.Diagnostics.Process.Start("explorer.exe", path);
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                System.Diagnostics.Process.Start("open", path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open file explorer for path: {Path}", path);
        }
    }

    public void Show()
    {
        Logger.Debug("RecentProjectsWindow.Show() called, setting _isOpen = true");
        _isOpen = true;
    }
}
