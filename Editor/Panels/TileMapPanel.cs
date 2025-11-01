using System.Numerics;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels;

/// <summary>
/// TileMap editor panel with Godot-like functionality
/// </summary>
public class TileMapPanel
{
    private TileMapComponent? _activeTileMap;
    private TileSet? _tileSet;
    private int _selectedTileId = -1;
    private TileMapTool _currentTool = TileMapTool.Paint;
    private bool _showGrid = true;
    private Vector4 _gridColor = new Vector4(1, 1, 1, 0.3f);
    private bool _hasBeenDockedOnce = false;

    // Painting state
    private bool _isPainting;
    private int _lastPaintX = -1;
    private int _lastPaintY = -1;

    // Camera/viewport
    private Vector2 _viewportPos;
    private Vector2 _viewportSize;
    private float _zoom = 1.0f;
    private Vector2 _panOffset = Vector2.Zero;

    public bool IsOpen { get; set; }

    public void SetTileMap(TileMapComponent? tileMap)
    {
        _activeTileMap = tileMap;
        if (tileMap != null && !string.IsNullOrEmpty(tileMap.TileSetPath))
        {
            LoadTileSet(tileMap.TileSetPath, tileMap.TileSetColumns, tileMap.TileSetRows);
        }
        IsOpen = true; // Automatically open when a tilemap is set
    }

    private void LoadTileSet(string path, int columns, int rows)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"TileSet texture not found: {path}");
            return;
        }

        _tileSet = new TileSet
        {
            TexturePath = path,
            Columns = columns,
            Rows = rows
        };
        _tileSet.LoadTexture();
        
        if (_tileSet.Texture == null)
        {
            Console.WriteLine($"Failed to load TileSet texture: {path}");
            return;
        }
        
        // Calculate tile dimensions from texture size
        _tileSet.TileWidth = _tileSet.Texture.Width / columns;
        _tileSet.TileHeight = _tileSet.Texture.Height / rows;
        
        _tileSet.GenerateTiles();
        
        Console.WriteLine($"TileSet loaded: {path}");
        Console.WriteLine($"  Texture size: {_tileSet.Texture.Width}x{_tileSet.Texture.Height}");
        Console.WriteLine($"  Grid: {columns}x{rows} = {_tileSet.Tiles.Count} tiles");
        Console.WriteLine($"  Tile size: {_tileSet.TileWidth}x{_tileSet.TileHeight}");
    }

    public void OnImGuiRender(uint viewportDockId = 0)
    {
        if (!IsOpen) return;

        if (_activeTileMap == null)
        {
            IsOpen = false;
            return;
        }

        // Dock to Viewport on first open
        if (!_hasBeenDockedOnce && viewportDockId != 0)
        {
            ImGui.SetNextWindowDockID(viewportDockId);
            _hasBeenDockedOnce = true;
        }

        bool isOpen = true;
        if (ImGui.Begin("TileMap Editor", ref isOpen))
        {
            DrawToolbar();
            ImGui.Separator();
            
            DrawLayerPanel();
            ImGui.Separator();

            if (ImGui.BeginChild("TileMapContent", new Vector2(0, 0)))
            {
                DrawTileSetPalette();
                ImGui.SameLine();
                DrawTileMapCanvas();
                ImGui.EndChild();
            }
        }
        ImGui.End();

        // Update IsOpen state based on window close button
        if (!isOpen)
        {
            IsOpen = false;
            _hasBeenDockedOnce = false; // Reset docking flag when window closes
        }
    }

    private void DrawToolbar()
    {
        ImGui.Text("Tools:");
        ImGui.SameLine();

        if (ImGui.Button("Paint"))
            _currentTool = TileMapTool.Paint;
        ImGui.SameLine();

        if (ImGui.Button("Erase"))
            _currentTool = TileMapTool.Erase;
        ImGui.SameLine();

        if (ImGui.Button("Fill"))
            _currentTool = TileMapTool.Fill;
        ImGui.SameLine();

        if (ImGui.Button("Select"))
            _currentTool = TileMapTool.Select;

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(20, 0));
        ImGui.SameLine();

        ImGui.Checkbox("Show Grid", ref _showGrid);

        ImGui.Text($"Current Tool: {_currentTool}");
    }

    private void DrawLayerPanel()
    {
        if (_activeTileMap == null) return;

        ImGui.Text("Layers:");
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("Click to select layer for painting");
            ImGui.Text("Eye icon toggles visibility");
            ImGui.Text("Use arrows to reorder layers");
            ImGui.EndTooltip();
        }

        if (ImGui.BeginChild("LayersList", new Vector2(0, 120), ImGuiChildFlags.Border))
        {
            for (int i = _activeTileMap.Layers.Count - 1; i >= 0; i--) // Draw from top to bottom
            {
                var layer = _activeTileMap.Layers[i];

                ImGui.PushID(i);

                // Visibility toggle (eye icon)
                bool visible = layer.Visible;
                if (ImGui.Checkbox(visible ? "👁" : "  ", ref visible))
                {
                    layer.Visible = visible;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(visible ? "Hide layer" : "Show layer");
                    ImGui.EndTooltip();
                }

                ImGui.SameLine();

                // Layer selection
                bool isSelected = i == _activeTileMap.ActiveLayerIndex;

                // Highlight selected layer with background color
                if (isSelected)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.3f, 0.5f, 0.8f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.4f, 0.6f, 0.9f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.5f, 0.7f, 1.0f, 0.8f));
                }

                if (ImGui.Selectable($"{layer.Name} (Z:{layer.ZIndex})##layer{i}", isSelected, ImGuiSelectableFlags.None, new Vector2(0, 20)))
                {
                    _activeTileMap.ActiveLayerIndex = i;
                }

                if (isSelected)
                {
                    ImGui.PopStyleColor(3);
                }

                // Layer reordering buttons
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 60);

                // Move up button
                if (i < _activeTileMap.Layers.Count - 1)
                {
                    if (ImGui.SmallButton("↑"))
                    {
                        // Swap with layer above
                        var temp = _activeTileMap.Layers[i];
                        _activeTileMap.Layers[i] = _activeTileMap.Layers[i + 1];
                        _activeTileMap.Layers[i + 1] = temp;

                        // Update Z-indices
                        _activeTileMap.Layers[i].ZIndex = i;
                        _activeTileMap.Layers[i + 1].ZIndex = i + 1;

                        // Update active layer index if needed
                        if (_activeTileMap.ActiveLayerIndex == i)
                            _activeTileMap.ActiveLayerIndex = i + 1;
                        else if (_activeTileMap.ActiveLayerIndex == i + 1)
                            _activeTileMap.ActiveLayerIndex = i;
                    }
                }
                else
                {
                    ImGui.Dummy(new Vector2(20, 0));
                }

                ImGui.SameLine();

                // Move down button
                if (i > 0)
                {
                    if (ImGui.SmallButton("↓"))
                    {
                        // Swap with layer below
                        var temp = _activeTileMap.Layers[i];
                        _activeTileMap.Layers[i] = _activeTileMap.Layers[i - 1];
                        _activeTileMap.Layers[i - 1] = temp;

                        // Update Z-indices
                        _activeTileMap.Layers[i].ZIndex = i;
                        _activeTileMap.Layers[i - 1].ZIndex = i - 1;

                        // Update active layer index if needed
                        if (_activeTileMap.ActiveLayerIndex == i)
                            _activeTileMap.ActiveLayerIndex = i - 1;
                        else if (_activeTileMap.ActiveLayerIndex == i - 1)
                            _activeTileMap.ActiveLayerIndex = i;
                    }
                }
                else
                {
                    ImGui.Dummy(new Vector2(20, 0));
                }

                ImGui.PopID();
            }
            ImGui.EndChild();
        }

        // Layer controls
        if (ImGui.Button("➕ Add Layer"))
        {
            _activeTileMap.AddLayer($"Layer {_activeTileMap.Layers.Count}");
        }
        ImGui.SameLine();

        if (_activeTileMap.Layers.Count > 1)
        {
            if (ImGui.Button("🗑 Remove Layer"))
            {
                _activeTileMap.RemoveLayer(_activeTileMap.ActiveLayerIndex);
            }
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("🗑 Remove Layer");
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                ImGui.Text("Cannot remove the last layer");
                ImGui.EndTooltip();
            }
        }

        ImGui.SameLine();

        // Layer rename button
        if (ImGui.Button("✏ Rename"))
        {
            ImGui.OpenPopup("RenameLayerPopup");
        }

        // Rename popup
        if (ImGui.BeginPopup("RenameLayerPopup"))
        {
            ImGui.Text("Rename Layer:");
            ImGui.Separator();

            var currentLayer = _activeTileMap.Layers[_activeTileMap.ActiveLayerIndex];
            var layerName = currentLayer.Name;

            if (ImGui.InputText("##layername", ref layerName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (!string.IsNullOrWhiteSpace(layerName))
                {
                    currentLayer.Name = layerName;
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("OK"))
            {
                if (!string.IsNullOrWhiteSpace(layerName))
                {
                    currentLayer.Name = layerName;
                }
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        // Show active layer info
        ImGui.Separator();
        ImGui.Text($"Active: {_activeTileMap.Layers[_activeTileMap.ActiveLayerIndex].Name}");
        ImGui.SameLine();

        // Opacity slider for active layer
        var activeLayer = _activeTileMap.Layers[_activeTileMap.ActiveLayerIndex];
        float opacity = activeLayer.Opacity;
        ImGui.SetNextItemWidth(100);
        if (ImGui.SliderFloat("Opacity", ref opacity, 0.0f, 1.0f, "%.2f"))
        {
            activeLayer.Opacity = opacity;
        }
    }

    private void DrawTileSetPalette()
    {
        if (_tileSet?.Texture == null)
        {
            ImGui.BeginChild("TileSetPalette", new Vector2(250, 0), ImGuiChildFlags.Border);
            ImGui.Text("No TileSet loaded");
            ImGui.EndChild();
            return;
        }

        ImGui.BeginChild("TileSetPalette", new Vector2(250, 0), ImGuiChildFlags.Border);
        ImGui.Text($"TileSet Palette ({_tileSet.Tiles.Count} tiles)");
        ImGui.Text($"{_tileSet.Columns}x{_tileSet.Rows}");
        ImGui.Separator();

        var textureId = (nint)_tileSet.Texture.GetRendererId();
        var tileDisplaySize = new Vector2(32, 32);
        var spacing = 2.0f;
        
        // Calculate how many tiles fit per row in the palette window (with some padding)
        var availableWidth = ImGui.GetContentRegionAvail().X;
        int tilesPerRow = Math.Max(1, (int)((availableWidth - 10) / (tileDisplaySize.X + spacing)));

        for (int i = 0; i < _tileSet.Tiles.Count; i++)
        {
            ImGui.PushID(i);

            var texCoords = _tileSet.GetTileTextureCoords(i);
            var uvMin = texCoords[0];
            var uvMax = texCoords[2];
            
            bool isSelected = _selectedTileId == i;
            var bgColor = isSelected ? new Vector4(0.3f, 0.5f, 0.8f, 1.0f) : new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            
            var cursorPos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            
            drawList.AddRectFilled(cursorPos, cursorPos + tileDisplaySize, ImGui.ColorConvertFloat4ToU32(bgColor));
            
            ImGui.Image(textureId, tileDisplaySize, uvMin, uvMax);
            
            if (ImGui.IsItemClicked())
            {
                _selectedTileId = i;
            }
            
            // Show tile ID on hover
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Tile ID: {i}");
                ImGui.Text($"Position: ({i % _tileSet.Columns}, {i / _tileSet.Columns})");
                ImGui.EndTooltip();
            }

            if ((i + 1) % tilesPerRow != 0)
            {
                ImGui.SameLine();
            }

            ImGui.PopID();
        }

        ImGui.EndChild();
    }

    private void DrawTileMapCanvas()
    {
        if (_activeTileMap == null) return;

        ImGui.BeginChild("TileMapCanvas", new Vector2(0, 0), ImGuiChildFlags.Border, 
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        _viewportPos = ImGui.GetCursorScreenPos();
        _viewportSize = ImGui.GetContentRegionAvail();

        var drawList = ImGui.GetWindowDrawList();

        // Draw background
        drawList.AddRectFilled(_viewportPos, _viewportPos + _viewportSize, 
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.15f, 1.0f)));

        // Calculate tile display size - use fixed pixel size for editor, not world-space TileSize
        // The TileSize property is for runtime world units, editor should use consistent pixel size
        var baseTilePixelSize = 32.0f; // Base display size in pixels
        var tileDisplaySize = new Vector2(baseTilePixelSize, baseTilePixelSize) * _zoom;

        // Draw tiles
        DrawTiles(drawList, tileDisplaySize);

        // Draw grid
        if (_showGrid)
        {
            DrawGrid(drawList, tileDisplaySize);
        }

        // Handle input
        HandleCanvasInput(tileDisplaySize);

        ImGui.EndChild();
    }

    private void DrawTiles(ImDrawListPtr drawList, Vector2 tileDisplaySize)
    {
        if (_activeTileMap == null || _tileSet?.Texture == null) return;

        var textureId = (nint)_tileSet.Texture.GetRendererId();

        foreach (var layer in _activeTileMap.Layers.OrderBy(l => l.ZIndex))
        {
            if (!layer.Visible) continue;

            for (int y = 0; y < _activeTileMap.Height; y++)
            {
                for (int x = 0; x < _activeTileMap.Width; x++)
                {
                    int tileId = layer.Tiles[x, y];
                    if (tileId < 0) continue;

                    var screenPos = WorldToScreen(new Vector2(x, y), tileDisplaySize);
                    var texCoords = _tileSet.GetTileTextureCoords(tileId);
                    var uvMin = texCoords[0];
                    var uvMax = texCoords[2];

                    var color = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, layer.Opacity));
                    drawList.AddImage(textureId, screenPos, screenPos + tileDisplaySize, uvMin, uvMax, color);
                }
            }
        }
    }

    private void DrawGrid(ImDrawListPtr drawList, Vector2 tileDisplaySize)
    {
        if (_activeTileMap == null) return;

        var gridColorU32 = ImGui.ColorConvertFloat4ToU32(_gridColor);

        // Vertical lines
        for (int x = 0; x <= _activeTileMap.Width; x++)
        {
            var start = WorldToScreen(new Vector2(x, 0), tileDisplaySize);
            var end = WorldToScreen(new Vector2(x, _activeTileMap.Height), tileDisplaySize);
            drawList.AddLine(start, end, gridColorU32);
        }

        // Horizontal lines
        for (int y = 0; y <= _activeTileMap.Height; y++)
        {
            var start = WorldToScreen(new Vector2(0, y), tileDisplaySize);
            var end = WorldToScreen(new Vector2(_activeTileMap.Width, y), tileDisplaySize);
            drawList.AddLine(start, end, gridColorU32);
        }
    }

    private void HandleCanvasInput(Vector2 tileDisplaySize)
    {
        if (!ImGui.IsWindowHovered()) return;

        var io = ImGui.GetIO();
        var mousePos = io.MousePos;

        // Zoom with mouse wheel
        if (io.MouseWheel != 0)
        {
            _zoom += io.MouseWheel * 0.1f;
            _zoom = Math.Clamp(_zoom, 0.1f, 5.0f);
        }

        // Pan with middle mouse button
        if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
        {
            _panOffset += io.MouseDelta / _zoom;
        }

        // Paint/Erase with left mouse button
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            var tileCoord = ScreenToTile(mousePos, tileDisplaySize);
            
            if (tileCoord.X >= 0 && tileCoord.X < _activeTileMap!.Width &&
                tileCoord.Y >= 0 && tileCoord.Y < _activeTileMap.Height)
            {
                // Only paint if we moved to a new tile (prevents spam)
                if (!_isPainting || tileCoord.X != _lastPaintX || tileCoord.Y != _lastPaintY)
                {
                    ApplyTool(tileCoord);
                    _isPainting = true;
                    _lastPaintX = tileCoord.X;
                    _lastPaintY = tileCoord.Y;
                }
            }
        }
        else
        {
            _isPainting = false;
        }
    }

    private void ApplyTool(Vector2Int tileCoord)
    {
        if (_activeTileMap == null) return;

        int layer = _activeTileMap.ActiveLayerIndex;

        switch (_currentTool)
        {
            case TileMapTool.Paint:
                if (_selectedTileId >= 0)
                {
                    _activeTileMap.SetTile(tileCoord.X, tileCoord.Y, _selectedTileId, layer);
                }
                break;

            case TileMapTool.Erase:
                _activeTileMap.SetTile(tileCoord.X, tileCoord.Y, -1, layer);
                break;

            case TileMapTool.Fill:
                FloodFill(tileCoord.X, tileCoord.Y, layer);
                break;
        }
    }

    private void FloodFill(int x, int y, int layer)
    {
        if (_activeTileMap == null || _selectedTileId < 0) return;

        int targetTile = _activeTileMap.GetTile(x, y, layer);
        if (targetTile == _selectedTileId) return;

        var stack = new Stack<(int x, int y)>();
        stack.Push((x, y));
        var visited = new HashSet<(int, int)>();

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();

            if (cx < 0 || cx >= _activeTileMap.Width || cy < 0 || cy >= _activeTileMap.Height)
                continue;

            if (visited.Contains((cx, cy)))
                continue;

            if (_activeTileMap.GetTile(cx, cy, layer) != targetTile)
                continue;

            visited.Add((cx, cy));
            _activeTileMap.SetTile(cx, cy, _selectedTileId, layer);

            stack.Push((cx + 1, cy));
            stack.Push((cx - 1, cy));
            stack.Push((cx, cy + 1));
            stack.Push((cx, cy - 1));
        }
    }

    private Vector2 WorldToScreen(Vector2 gridPos, Vector2 tileDisplaySize)
    {
        // gridPos is in tile grid coordinates (0,0), (1,0), etc.
        // tileDisplaySize already includes zoom, so don't apply it again
        return _viewportPos + _panOffset + gridPos * tileDisplaySize + _viewportSize * 0.5f;
    }

    private Vector2Int ScreenToTile(Vector2 screenPos, Vector2 tileDisplaySize)
    {
        var worldPos = screenPos - _viewportPos - _viewportSize * 0.5f - _panOffset;
        return new Vector2Int(
            (int)(worldPos.X / tileDisplaySize.X),
            (int)(worldPos.Y / tileDisplaySize.Y)
        );
    }
}

public enum TileMapTool
{
    Paint,
    Erase,
    Fill,
    Select
}

public struct Vector2Int
{
    public int X;
    public int Y;

    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }
}

