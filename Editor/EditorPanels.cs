using ECS;
using Editor.ComponentEditors;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Input;
using Editor.Panels;
using Engine.Renderer.Cameras;

namespace Editor;

public class EditorPanels(
    IConsolePanel consolePanel,
    IPropertiesPanel propertiesPanel,
    ISceneHierarchyPanel sceneHierarchyPanel,
    IContentBrowserPanel contentBrowserPanel,
    RendererStatsPanel rendererStatsPanel,
    RecentProjectsPanel recentProjectsPanel,
    KeyboardShortcutsPanel keyboardShortcutsPanel,
    PerformanceMonitorPanel performanceMonitor,
    ScriptComponentEditor scriptComponentEditor,
    GameComponentEditor gameComponentEditor)
{
    public IConsolePanel ConsolePanel { get; } = consolePanel;
    public IPropertiesPanel PropertiesPanel { get; } = propertiesPanel;
    public ISceneHierarchyPanel SceneHierarchyPanel { get; } = sceneHierarchyPanel;
    public IContentBrowserPanel ContentBrowserPanel { get; } = contentBrowserPanel;
    public RendererStatsPanel RendererStatsPanel { get; } = rendererStatsPanel;
    public RecentProjectsPanel RecentProjectsPanel { get; } = recentProjectsPanel;
    public KeyboardShortcutsPanel KeyboardShortcutsPanel { get; } = keyboardShortcutsPanel;
    public PerformanceMonitorPanel PerformanceMonitor { get; } = performanceMonitor;

    public void Draw(Entity? hoveredEntity, EditorCamera camera)
    {
        SceneHierarchyPanel.Draw();
        PropertiesPanel.Draw();
        ContentBrowserPanel.Draw();
        ConsolePanel.Draw();

        scriptComponentEditor.Draw();
        gameComponentEditor.RenderPopups();
        ContentBrowserPanel.RenderPopups();
        RecentProjectsPanel.Draw();
        KeyboardShortcutsPanel.Draw();

        var hoveredEntityName = hoveredEntity?.Name ?? "None";
        var camPos = camera.GetPosition();
        RendererStatsPanel.Draw(hoveredEntityName, camPos, camera.Yaw,
            () => PerformanceMonitor.RenderUI());
    }
}
