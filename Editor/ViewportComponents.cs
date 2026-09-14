using Editor.Features.Viewport;
using Editor.Features.Scene;

namespace Editor;

public class ViewportComponents(
    SceneToolbar sceneToolbar,
    ViewportToolManager viewportToolManager,
    ViewportRuler viewportRuler,
    ViewportGrid viewportGrid)
{
    public SceneToolbar SceneToolbar { get; } = sceneToolbar;
    public ViewportToolManager ViewportToolManager { get; } = viewportToolManager;
    public ViewportRuler ViewportRuler { get; } = viewportRuler;
    public ViewportGrid ViewportGrid { get; } = viewportGrid;
}
