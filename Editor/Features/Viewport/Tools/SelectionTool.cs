using System.Numerics;
using ECS;
using Editor.Features.Selection;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport.Tools;

public class SelectionTool(IEditorSelection selection) : IEntityHoverTool
{
    public EditorMode Mode => EditorMode.Select;
    public bool IsActive => false;

    public Entity? HoveredEntity { get; set; }

    public void OnActivate() { }

    public void OnDeactivate() { }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (HoveredEntity != null)
            selection.Select(HoveredEntity, SelectionSource.Viewport);
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera) { }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera) { }

    public void Render(Vector2[] viewportBounds, IViewCamera camera) { }
}
