using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.Tiled;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class TileMapComponentEditor(
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    TiledMapImportService tiledImport) : ComponentEditor<TileMapComponent>(history)
{
    protected override string DisplayName => "Tile Map";

    protected override void DrawContent(TileMapComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Source", component.SourceMapPath ?? "",
            newValue => component.SourceMapPath = (string)newValue);
        TextDrawer.DrawInfoText($"Width: {component.Width}");
        TextDrawer.DrawInfoText($"Height: {component.Height}");
        TextDrawer.DrawInfoText($"Tile Size: {component.TileSize}");
        TextDrawer.DrawInfoText($"Layers: {component.Layers.Count}");

        ButtonDrawer.DrawButton("Reimport", () => tiledImport.Reimport(entity),
            disabled: string.IsNullOrWhiteSpace(component.SourceMapPath));
    }
}
