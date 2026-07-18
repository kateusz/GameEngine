using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Input;
using Editor.Panels;
using Editor.Publisher;
using Engine.Core;
using ImGuiNET;
using Serilog;

namespace Editor.Features.Shell;

public class EditorMenuBar(
    IProjectManager projectManager,
    IEditorPreferences editorPreferences,
    EditorSettingsUI editorSettingsUI,
    ISceneManager sceneManager,
    NewProjectPopup newProjectPopup,
    SceneSettingsPopup sceneSettingsPopup,
    PublishSettingsUI publishSettingsUI,
    RecentProjectsPanel recentProjectsPanel,
    RendererStatsPanel rendererStatsPanel,
    KeyboardShortcutsPanel keyboardShortcutsPanel,
    ViewportComponents viewport,
    IEditorCameraController cameraController)
{
    private static readonly ILogger Logger = Log.ForContext<EditorMenuBar>();

    public void Render()
    {
        if (!ImGui.BeginMenuBar()) return;

        RenderFileMenu();
        RenderSceneMenu();
        RenderViewMenu();
        RenderSettingsMenu();
        RenderHelpMenu();
        RenderPublishMenu();

        ImGui.EndMenuBar();
    }

    private void RenderFileMenu()
    {
        if (!ImGui.BeginMenu("File")) return;

        if (ImGui.MenuItem("New Project"))
            newProjectPopup.ShowNewProjectPopup();
        if (ImGui.MenuItem("Open Project"))
            newProjectPopup.ShowOpenProjectPopup();

        ImGui.Separator();

        if (ImGui.MenuItem("Show Recent Projects"))
            recentProjectsPanel.Show();

        if (ImGui.BeginMenu("Recent Projects"))
        {
            var recentProjects = editorPreferences.GetRecentProjects();
            if (recentProjects.Count == 0)
            {
                ImGui.MenuItem("(No recent projects)", false);
            }
            else
            {
                foreach (var recent in recentProjects)
                {
                    if (ImGui.MenuItem($"{recent.Name}"))
                    {
                        if (!projectManager.TryOpenProject(recent.Path, out var error))
                            Logger.Warning("Failed to open recent project {Path}: {Error}", recent.Path, error);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(recent.Path);
                        ImGui.Text($"Last opened: {recent.LastOpened:yyyy-MM-dd HH:mm}");
                        ImGui.EndTooltip();
                    }
                }

                ImGui.Separator();
                if (ImGui.MenuItem("Clear Recent Projects"))
                    editorPreferences.ClearRecentProjects();
            }
            ImGui.EndMenu();
        }

        ImGui.Separator();
        if (ImGui.MenuItem("Exit"))
            Environment.Exit(0);
        ImGui.EndMenu();
    }

    private void RenderSceneMenu()
    {
        if (!ImGui.BeginMenu("Scene...")) return;

        if (ImGui.MenuItem("New", "Ctrl+N"))
            sceneSettingsPopup.ShowNewScenePopup();
        if (ImGui.MenuItem("Save", "Ctrl+S"))
            sceneManager.Save();
        ImGui.EndMenu();
    }

    private void RenderViewMenu()
    {
        if (!ImGui.BeginMenu("View")) return;

        if (ImGui.MenuItem("Reset Camera"))
            cameraController.ResetCamera();
        ImGui.Separator();
        if (ImGui.MenuItem("Show Rulers", null, viewport.ViewportRuler.Enabled))
            viewport.ViewportRuler.Enabled = !viewport.ViewportRuler.Enabled;
        if (ImGui.MenuItem("Show 2D Grid", null, viewport.SceneToolbar.ShowGrid))
            viewport.SceneToolbar.SetShowGrid(!viewport.SceneToolbar.ShowGrid);
        if (ImGui.MenuItem("Show 3D Grid", null, viewport.SceneToolbar.ShowGrid3D))
            viewport.SceneToolbar.SetShowGrid3D(!viewport.SceneToolbar.ShowGrid3D);
        if (ImGui.MenuItem("Show Stats", null, rendererStatsPanel.IsVisible))
            rendererStatsPanel.IsVisible = !rendererStatsPanel.IsVisible;
        ImGui.EndMenu();
    }

    private void RenderSettingsMenu()
    {
        if (!ImGui.BeginMenu("Settings")) return;

        if (ImGui.MenuItem("Editor Settings"))
            editorSettingsUI.Show();
        ImGui.EndMenu();
    }

    private void RenderHelpMenu()
    {
        if (!ImGui.BeginMenu("Help")) return;

        if (ImGui.MenuItem("Keyboard Shortcuts"))
            keyboardShortcutsPanel.Show();
        ImGui.EndMenu();
    }

    private void RenderPublishMenu()
    {
        if (!ImGui.BeginMenu("Publish")) return;

        if (ImGui.MenuItem("Build & Publish"))
            publishSettingsUI.ShowPublishModal();
        ImGui.EndMenu();
    }
}
