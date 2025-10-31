using ECS;
using Engine.Scene.Components;
using ImGuiNET;
using System.Numerics;
using Engine;

namespace Editor.Panels.ComponentEditors;

public class TileMapComponentEditor : IComponentEditor
{
    private readonly TileMapPanel _tileMapPanel;

    public TileMapComponentEditor(TileMapPanel tileMapPanel)
    {
        _tileMapPanel = tileMapPanel;
    }

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<TileMapComponent>("TileMap", entity, e =>
        {
            var component = e.GetComponent<TileMapComponent>();

            // Dimensions
            int width = component.Width;
            if (ImGui.DragInt("Width", ref width, 1, 1, 1000))
            {
                component.Resize(width, component.Height);
            }

            int height = component.Height;
            if (ImGui.DragInt("Height", ref height, 1, 1, 1000))
            {
                component.Resize(component.Width, height);
            }

            // Tile Size
            var tileSize = component.TileSize;
            if (ImGui.DragFloat2("Tile Size", ref tileSize, 0.1f, 0.1f, 100.0f))
            {
                component.TileSize = tileSize;
            }

            // TileSet Configuration
            ImGui.Separator();
            ImGui.Text("TileSet Configuration");

            // TileSet Path (file picker would be better)
            var tileSetPath = component.TileSetPath;
            if (ImGui.InputText("TileSet Path", ref tileSetPath, 512))
            {
                // TODO
                component.TileSetPath = AssetsManager.AssetsPath + "//textures//spritesheet." + tileSetPath;
            }

            if (ImGui.Button("Browse..."))
            {
                // TODO: Open file dialog
                ImGui.OpenPopup("FileBrowser");
            }

            int columns = component.TileSetColumns;
            if (ImGui.DragInt("Columns", ref columns, 1, 1, 64))
            {
                component.TileSetColumns = columns;
            }

            int rows = component.TileSetRows;
            if (ImGui.DragInt("Rows", ref rows, 1, 1, 64))
            {
                component.TileSetRows = rows;
            }

            // Layers info
            ImGui.Separator();
            ImGui.Text($"Layers: {component.Layers.Count}");
            ImGui.Text($"Active Layer: {component.ActiveLayerIndex}");

            // Open TileMap Editor button
            ImGui.Separator();
            if (ImGui.Button("Open TileMap Editor", new Vector2(-1, 30)))
            {
                _tileMapPanel.SetTileMap(component);
            }
        });
    }
}

