using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Input;
using Editor.Panels;

namespace Editor;

public class EditorPanels(
    IConsolePanel consolePanel,
    IPropertiesPanel propertiesPanel,
    ISceneHierarchyPanel sceneHierarchyPanel,
    IContentBrowserPanel contentBrowserPanel,
    RendererStatsPanel rendererStatsPanel,
    IAnimationTimelinePanel animationTimeline,
    RecentProjectsPanel recentProjectsPanel,
    KeyboardShortcutsPanel keyboardShortcutsPanel,
    PerformanceMonitorPanel performanceMonitor)
{
    public IConsolePanel ConsolePanel { get; } = consolePanel;
    public IPropertiesPanel PropertiesPanel { get; } = propertiesPanel;
    public ISceneHierarchyPanel SceneHierarchyPanel { get; } = sceneHierarchyPanel;
    public IContentBrowserPanel ContentBrowserPanel { get; } = contentBrowserPanel;
    public RendererStatsPanel RendererStatsPanel { get; } = rendererStatsPanel;
    public IAnimationTimelinePanel AnimationTimeline { get; } = animationTimeline;
    public RecentProjectsPanel RecentProjectsPanel { get; } = recentProjectsPanel;
    public KeyboardShortcutsPanel KeyboardShortcutsPanel { get; } = keyboardShortcutsPanel;
    public PerformanceMonitorPanel PerformanceMonitor { get; } = performanceMonitor;
}
