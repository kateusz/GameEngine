using System.Numerics;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport.Tools;

public interface IViewportTool
{
    EditorMode Mode { get; }
    bool IsActive { get; }
    void OnActivate();
    void OnDeactivate();
    void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera);
    void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera);
    void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera);
    void Render(Vector2[] viewportBounds, IViewCamera camera);
}
