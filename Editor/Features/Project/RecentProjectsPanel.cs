using System.Numerics;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Panels;
using Editor.UI.Drawers;
using Engine.Core.Window;
using ImGuiNET;
using Serilog;

namespace Editor.Features.Project;

public class RecentProjectsPanel(
    IEditorPreferences editorPreferences,
    IProjectManager projectManager,
    NewProjectPopup newProjectPopup,
    EditorSceneLoadService sceneLoadService,
    UnsavedSceneGuard unsavedSceneGuard) : IEditorPanel
{
    private static readonly ILogger Logger = Log.ForContext<RecentProjectsPanel>();

    private bool _isOpen = true;
    private bool _awaitingSceneAfterOpen;
    private string _loadingProjectName = string.Empty;
    private string? _projectToRemove;
    private string? _pendingOpenPath;
    private float _loadingSpinnerRotation;

    public bool IsLoading { get; private set; }

    /// <summary>Queue a project open on the next frame (safe from ImGui menus).</summary>
    public void QueueOpen(string path, string? displayName = null)
    {
        if (!Directory.Exists(path))
        {
            Logger.Warning("Project directory not found: {Path}", path);
            return;
        }

        unsavedSceneGuard.Run(() => BeginOpen(path, displayName ?? Path.GetFileName(path)));
    }

    private void BeginOpen(string path, string displayName)
    {
        _pendingOpenPath = path;
        _loadingProjectName = displayName;
        _loadingSpinnerRotation = 0.0f;
        IsLoading = true;
    }

    public void Draw()
    {
        if (_pendingOpenPath is { } pendingPath)
            ProcessPendingOpen(pendingPath);

        if (IsLoading && _awaitingSceneAfterOpen && !sceneLoadService.IsLoading)
        {
            IsLoading = false;
            _awaitingSceneAfterOpen = false;
        }

        if (_isOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(DisplayConfig.StandardPopupSize.Width, 0), ImGuiCond.Appearing);

            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(
                new Vector2(viewport.Pos.X + viewport.Size.X * 0.5f, viewport.Pos.Y + viewport.Size.Y * 0.5f),
                ImGuiCond.Appearing,
                new Vector2(0.5f, 0.5f)
            );

            var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
                | ImGuiWindowFlags.AlwaysAutoResize;

            if (ImGui.Begin("Recent Projects", ref _isOpen, windowFlags))
            {
                if (!IsLoading)
                {
                    DrawRecentProjects();
                    ImGui.Separator();
                    DrawQuickActions();
                }
            }
            ImGui.End();
        }

        if (IsLoading)
        {
            var text = sceneLoadService is { IsLoading: true, LoadingName: { } scene }
                ? $"Loading {_loadingProjectName} and scene {scene}..."
                : $"Loading {_loadingProjectName}...";
            LoadingOverlayDrawer.DrawFullscreen(text, ref _loadingSpinnerRotation);
        }

        if (_projectToRemove != null)
        {
            editorPreferences.RemoveRecentProject(_projectToRemove);
            _projectToRemove = null;
        }
    }

    private void DrawRecentProjects()
    {
        var recentProjects = editorPreferences.GetRecentProjects()
            .Where(p => Directory.Exists(p.Path))
            .ToList();

        if (recentProjects.Count == 0)
        {
            TextDrawer.DrawWarningText("No recent projects found. Create a new project or open an existing one to get started.");
            return;
        }

        const float cardHeight = 56f;
        const float maxListHeight = 280f;
        var listHeight = System.Math.Min(recentProjects.Count * cardHeight, maxListHeight);

        if (ImGui.BeginChild("ProjectsList", new Vector2(0, listHeight), ImGuiChildFlags.Border))
        {
            for (var i = 0; i < recentProjects.Count; i++)
                DrawProjectItem(recentProjects[i], i);
        }
        ImGui.EndChild();
    }

    private void DrawProjectItem(RecentProject project, int index)
    {
        ImGui.PushID(index);

        var cursorPos = ImGui.GetCursorScreenPos();
        var cardSize = new Vector2(ImGui.GetContentRegionAvail().X, 52);
        var drawList = ImGui.GetWindowDrawList();

        var bgColor = ImGui.IsMouseHoveringRect(cursorPos, cursorPos + cardSize)
            ? ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.4f))
            : ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.3f));

        drawList.AddRectFilled(cursorPos, cursorPos + cardSize, bgColor, 4.0f);

        ImGui.BeginGroup();
        ImGui.Spacing();
        ImGui.Indent(10);

        ImGui.Text(project.Name);
        TextDrawer.DrawColoredText(project.Path, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));

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

        QueueOpen(project.Path, project.Name);
    }

    private void ProcessPendingOpen(string path)
    {
        _pendingOpenPath = null;

        try
        {
            // Must run on the UI thread — TryOpenProject disposes scenes and loads GL-backed assets.
            if (projectManager.TryOpenProject(path, out var error))
            {
                Logger.Information("Opened project from recent list: {Path}", path);
                _isOpen = false;
                _awaitingSceneAfterOpen = sceneLoadService.IsLoading;
                if (!_awaitingSceneAfterOpen)
                    IsLoading = false;
            }
            else
            {
                Logger.Error("Failed to open project {Path}: {Error}", path, error);
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open project {Path}", path);
            IsLoading = false;
        }
    }

    private void DrawQuickActions()
    {
        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;

        ButtonDrawer.DrawModalButton("New Project...", () =>
        {
            unsavedSceneGuard.Run(() =>
            {
                newProjectPopup.ShowNewProjectPopup();
                _isOpen = false;
            });
        }, buttonWidth, 20);

        ImGui.SameLine();

        ButtonDrawer.DrawModalButton("Open Project...", () => unsavedSceneGuard.Run(OpenProject), buttonWidth, 20);

        ButtonDrawer.DrawModalButton("Continue Without Project", () =>
        {
            _isOpen = false;
        }, ImGui.GetContentRegionAvail().X, 20);
    }

    private void OpenProject()
    {
        if (newProjectPopup.ShowOpenProjectPopup())
            _isOpen = false;
        else if (!OperatingSystem.IsWindows())
            _isOpen = false;
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
