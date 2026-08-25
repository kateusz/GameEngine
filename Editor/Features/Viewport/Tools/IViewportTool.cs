using System.Numerics;

namespace Editor.Features.Viewport.Tools;

public interface IViewportTool
{
    EditorMode Mode { get; }
    bool IsActive { get; }
    void OnActivate();
    void OnDeactivate();
    void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera);
    void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera);
    void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera);
    void Render(Vector2[] viewportBounds, EditorCamera camera);
}
