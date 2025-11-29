using ECS;
using Editor.ComponentEditors.Core;
using Editor.Panels;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Scene.Components;
using ImGuiNET;
using Serilog;

namespace Editor.ComponentEditors;

public class TileMapComponentEditor(TileMapPanel tileMapPanel, IAssetsManager assetsManager) : IComponentEditor
{
    private static readonly ILogger Logger = Log.ForContext<TileMapComponentEditor>();

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<TileMapComponent>("TileMap", entity, e =>
        {
            var component = e.GetComponent<TileMapComponent>();

            // Dimensions
            var width = component.Width;
            if (ImGui.DragInt("Width", ref width, 1, 1, 1000))
            {
                component.Resize(width, component.Height);
            }

            var height = component.Height;
            if (ImGui.DragInt("Height", ref height, 1, 1, 1000))
            {
                component.Resize(component.Width, height);
            }

            var tileSize = component.TileSize;
            if (ImGui.DragFloat2("Tile Size", ref tileSize, 0.1f, 0.1f, 100.0f))
            {
                component.TileSize = tileSize;
                ReloadTileMapIfActive(component);
            }

            ImGui.Separator();
            UIPropertyRenderer.DrawPropertyRow("TileSet file", () =>
            {
                var tileSetPath = string.IsNullOrWhiteSpace(component.TileSetPath) ? "None" : component.TileSetPath;
                var tileSetName = Path.GetFileName(tileSetPath);
                ButtonDrawer.DrawFullWidthButton(tileSetName, () =>
                {
                    // TODO: Open file browser popup (Phase 3 enhancement)
                    Logger.Information("File browser not yet implemented");
                });

                DragDropDrawer.HandleFileDropTarget(
                    DragDropDrawer.ContentBrowserItemPayload,
                    path => !string.IsNullOrWhiteSpace(path) &&
                            DragDropDrawer.HasValidExtension(path, ".png"),
                    droppedPath =>
                    {
                        var tilemapPath = Path.Combine(assetsManager.AssetsPath, droppedPath);
                        component.TileSetPath = tilemapPath;
                        ReloadTileMapIfActive(component);
                    });
            });

            var columns = component.TileSetColumns;
            if (ImGui.DragInt("Columns", ref columns, 1, 1, 64))
            {
                component.TileSetColumns = columns;
                ReloadTileMapIfActive(component);
            }

            var rows = component.TileSetRows;
            if (ImGui.DragInt("Rows", ref rows, 1, 1, 64))
            {
                component.TileSetRows = rows;
                ReloadTileMapIfActive(component);
            }

            // Layers info
            ImGui.Separator();
            ImGui.Text($"Layers: {component.Layers.Count}");
            ImGui.Text($"Active Layer Index: {component.ActiveLayerIndex}");

            // Open TileMap Editor button
            ImGui.Separator();
            ButtonDrawer.DrawButton("Open TileMap Editor", -1, 30, () =>
            {
                tileMapPanel.SetTileMap(component);
            });
        });
    }
    
    private void ReloadTileMapIfActive(TileMapComponent component)
    {
        if (tileMapPanel.IsOpen && tileMapPanel.IsActiveFor(component))
        {
            tileMapPanel.ReloadTileSet();
        }
    }
}

