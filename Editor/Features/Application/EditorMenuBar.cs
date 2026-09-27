using Editor.Commands;
using Editor.Features.Project;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Panels;
using ImGuiNET;

namespace Editor.Features.Application;

public class EditorMenuBar(
    CommandRegistry registry,
    IEditorPreferences editorPreferences,
    RecentProjectsPanel recentProjectsPanel,
    RendererStatsPanel rendererStatsPanel,
    ViewportComponents viewport)
{
    public void Render()
    {
        if (!ImGui.BeginMenuBar()) return;

        RenderProjectMenu();
        RenderSceneMenu();
        RenderViewMenu();
        RenderEditorMenu();
        RenderHelpMenu();

        ImGui.EndMenuBar();
    }

    private void RenderProjectMenu()
    {
        if (!ImGui.BeginMenu("Project")) return;
        
        CommandItem("New...", EditorCommandIds.NewProject);
        CommandItem("Open...", EditorCommandIds.OpenProject);
        CommandItem("Close", EditorCommandIds.CloseProject);
        CommandItem("Show Recent Projects", EditorCommandIds.ShowRecentProjects);

        if (ImGui.BeginMenu("Recent Projects"))
        {
            var recentProjects = editorPreferences.GetRecentProjects()
                .Where(p => Directory.Exists(p.Path))
                .ToList();
            if (recentProjects.Count == 0)
            {
                ImGui.MenuItem("(No recent projects)", false);
            }
            else
            {
                foreach (var recent in recentProjects)
                {
                    if (ImGui.MenuItem($"{recent.Name}"))
                        recentProjectsPanel.QueueOpen(recent.Path, recent.Name);

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(recent.Path);
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

        CommandItem("Settings...", EditorCommandIds.ShowProjectSettings);
        CommandItem("Export...", EditorCommandIds.Export);

        ImGui.EndMenu();
    }

    private void RenderSceneMenu()
    {
        if (!ImGui.BeginMenu("Scene")) return;

        CommandItem("New...", EditorCommandIds.NewScene, "Ctrl+N");
        CommandItem("Open...", EditorCommandIds.OpenScene);
        CommandItem("Save", EditorCommandIds.SaveScene, "Ctrl+S");
        CommandItem("Close", EditorCommandIds.CloseScene);

        ImGui.Separator();
        CommandItem("Settings...", EditorCommandIds.SceneSettings);

        ImGui.EndMenu();
    }

    private void RenderViewMenu()
    {
        if (!ImGui.BeginMenu("View")) return;

        CommandItem("Command Palette", EditorCommandIds.OpenCommandPalette, "Ctrl+Shift+P");
        CommandItem("Reset Camera", EditorCommandIds.ResetCamera);
        ImGui.Separator();
        if (ImGui.MenuItem("Show Rulers", null, viewport.ViewportRuler.Enabled))
            registry.Execute(EditorCommandIds.ToggleRulers);
        if (ImGui.MenuItem("Show Debug", null, rendererStatsPanel.IsVisible))
            registry.Execute(EditorCommandIds.ToggleStats);
        ImGui.EndMenu();
    }

    private void RenderEditorMenu()
    {
        if (!ImGui.BeginMenu("Editor")) return;

        CommandItem("Settings...", EditorCommandIds.ShowEditorSettings);
        ImGui.EndMenu();
    }

    private void RenderHelpMenu()
    {
        if (!ImGui.BeginMenu("Help")) return;

        CommandItem("Keyboard Shortcuts...", EditorCommandIds.ShowKeyboardShortcuts);
        ImGui.EndMenu();
    }

    private void CommandItem(string label, string id, string? shortcut = null)
    {
        var can = registry.GetCanExecute(id);
        if (ImGui.MenuItem(label, shortcut, selected: false, enabled: can.Allowed))
            registry.Execute(id);
    }
}
