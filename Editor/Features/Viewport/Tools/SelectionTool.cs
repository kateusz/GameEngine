using System.Numerics;
using ECS;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// This is the default tool that allows clicking to select entities without manipulation.
/// </summary>
public class SelectionTool : IEntityHoverTool
{
    public event Action<Entity>? OnEntitySelected;

    public EditorMode Mode => EditorMode.Select;
    public bool IsActive => false; // not needed in this scenario
    
    public Entity? HoveredEntity { get; set; }

    public void OnActivate()
    {
    }

    public void OnDeactivate()
    {
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (HoveredEntity != null)
        {
            OnEntitySelected?.Invoke(HoveredEntity);
        }
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Selection tool doesn't need mouse move handling
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // No action needed on mouse up for selection tool
    }

    public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // SelectionTool doesn't render any overlays
    }
}
