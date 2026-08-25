using System.Numerics;
using ECS;
using Editor.Features.Selection;
using Serilog;

namespace Editor.Features.Viewport.Tools;

public class SelectionTool(IEditorSelection selection) : IEntityHoverTool
{
    private static readonly ILogger Logger = Log.ForContext<SelectionTool>();

    public EditorMode Mode => EditorMode.Select;
    public bool IsActive => false;

    public Entity? HoveredEntity { get; set; }

    public void OnActivate() { }

    public void OnDeactivate() { }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera)
    {
        Logger.Information(
            "[TilePick] SelectionTool down hover={Hover} mouse=({X:0.#},{Y:0.#})",
            HoveredEntity is null ? "null" : $"{HoveredEntity.Id} '{HoveredEntity.Name}'",
            mousePos.X, mousePos.Y);
        if (HoveredEntity != null)
            selection.Select(HoveredEntity, SelectionSource.Viewport);
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera) { }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, EditorCamera camera) { }

    public void Render(Vector2[] viewportBounds, EditorCamera camera) { }
}
