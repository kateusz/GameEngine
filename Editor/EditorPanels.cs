using ECS;
using Editor.ComponentEditors;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Input;
using Editor.Panels;
using Engine.Renderer.Cameras;

namespace Editor;

public class EditorPanels(
    IEnumerable<IEditorPanel> panels,
    IContentBrowserPanel contentBrowserPanel,
    RendererStatsPanel rendererStatsPanel,
    PerformanceMonitorPanel performanceMonitor,
    ScriptComponentEditor scriptComponentEditor,
    GameComponentEditor gameComponentEditor)
{
    public void Draw(Entity? hoveredEntity, EditorCamera camera)
    {
        foreach (var panel in panels)
            panel.Draw();

        scriptComponentEditor.Draw();
        gameComponentEditor.RenderPopups();
        contentBrowserPanel.RenderPopups();

        var hoveredEntityName = hoveredEntity?.Name ?? "None";
        var camPos = camera.GetPosition();
        rendererStatsPanel.Draw(hoveredEntityName, camPos, camera.Yaw,
            performanceMonitor.RenderUI);
    }
}
