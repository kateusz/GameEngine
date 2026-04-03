using Editor.Features.Viewport;
using Editor.Features.Viewport.Tools;
using Editor.Features.Scene;

namespace Editor;

public class ViewportComponents(
    SceneToolbar sceneToolbar,
    ViewportToolManager viewportToolManager,
    ViewportRuler viewportRuler,
    ViewportGrid viewportGrid,
    ViewportGrid3D viewportGrid3D)
{
    public SceneToolbar SceneToolbar { get; } = sceneToolbar;
    public ViewportToolManager ViewportToolManager { get; } = viewportToolManager;
    public ViewportRuler ViewportRuler { get; } = viewportRuler;
    public ViewportGrid ViewportGrid { get; } = viewportGrid;
    public ViewportGrid3D ViewportGrid3D { get; } = viewportGrid3D;
}
