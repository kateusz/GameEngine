# TileMap Editor System

## Overview

A complete TileMap editor system for the game engine, inspired by Godot's tilemap workflow. This system provides a visual tile-based level editing experience with multi-layer support, tileset management, and intuitive painting tools.

## Architecture

### Core Components

1. **TileMapComponent** (`Engine/Scene/Components/TileMapComponent.cs`)
   - Component for storing tilemap data on entities
   - Multi-layer support with visibility and opacity control
   - Configurable tile dimensions and grid size
   - Supports resizing without data loss

2. **TileSet** (`Engine/Scene/Components/TileSet.cs`)
   - Manages tile collections from texture atlases
   - Automatic UV coordinate calculation
   - Tile metadata and custom properties support
   - Configurable spacing and margins

3. **TileMapRenderSystem** (`Engine/Scene/Systems/TileMapRenderSystem.cs`)
   - ECS system for rendering tilemaps
   - Layer-based rendering with Z-ordering
   - Texture caching for performance
   - Batched rendering using Graphics2D

4. **TileMapPanel** (`Editor/Panels/TileMapPanel.cs`)
   - Visual tilemap editor window
   - Tile palette browser
   - Interactive canvas with pan/zoom
   - Layer management UI

5. **TileMapComponentEditor** (`Editor/Panels/ComponentEditors/TileMapComponentEditor.cs`)
   - Properties panel integration
   - TileMap configuration UI
   - Launch point for the visual editor

## Features

### Multi-Layer System
- Create unlimited layers
- Layer visibility toggles
- Opacity control per layer
- Z-index ordering for rendering

### Painting Tools
- **Paint Tool**: Click to place tiles
- **Erase Tool**: Remove tiles
- **Fill Tool**: Flood-fill with selected tile
- **Select Tool**: Future implementation for tile selection

### Editor Workflow
- Visual tile palette with preview
- Grid overlay for precise placement
- Pan (middle mouse) and zoom (scroll wheel)
- Real-time preview in viewport
- Brush painting with drag support

### Performance
- Texture atlas caching
- Efficient batched rendering
- Only visible layers are rendered
- Minimal memory overhead

## Usage

### 1. Create a TileMap Entity

In the Scene Hierarchy:
1. Create a new entity
2. Add Component → TileMap
3. Configure tilemap properties in Properties panel

### 2. Configure TileSet

In the TileMap component properties:
- **Width/Height**: Grid dimensions in tiles
- **Tile Size**: Size of each tile in world units (default: 1x1)
- **TileSet Path**: Path to your tileset texture atlas
- **Columns/Rows**: Grid layout of tiles in the texture

### 3. Open Visual Editor

Click the **"Open TileMap Editor"** button in the Properties panel.

### 4. Paint Your Level

**Tile Palette (Left Side)**
- Browse available tiles from your tileset
- Click to select a tile
- Selected tile highlighted in blue

**Canvas (Right Side)**
- Click/drag to paint tiles
- Middle mouse to pan
- Mouse wheel to zoom
- Grid overlay shows tile boundaries

**Layer Panel (Top)**
- Switch between layers
- Toggle layer visibility
- Add/remove layers

**Toolbar**
- Select painting tool (Paint/Erase/Fill)
- Toggle grid visibility

## Example TileSet Configuration

### Recommended Texture Format
- **Power-of-2 dimensions**: 256x256, 512x512, 1024x1024
- **Consistent tile sizes**: 16x16, 32x32, 64x64 pixels
- **No spacing/margin**: Or configure appropriately

### Example Setup
```
Texture: assets/tilesets/dungeon.png (512x512)
Tile Size: 64x64 pixels
Columns: 8 (512 / 64)
Rows: 8 (512 / 64)
Total Tiles: 64
```

## Integration Points

### Scene Serialization
TileMapComponent integrates with the scene serialization system. All tile data, layers, and configuration are saved with your scene.

### Component Editor Registry
The TileMapComponentEditor is registered in `ComponentEditorRegistry.cs`:
```csharp
{ typeof(TileMapComponent), tileMapComponentEditor }
```

### Component Selector
Available in "Add Component" menu as **TileMap**.

### Render Pipeline
TileMapRenderSystem renders at priority 190 (before sprites at 200) and is registered in `SceneSystemRegistry.cs`.

## API Reference

### TileMapComponent

```csharp
// Create and configure
var tileMap = entity.AddComponent<TileMapComponent>();
tileMap.Width = 20;
tileMap.Height = 15;
tileMap.TileSize = new Vector2(1.0f, 1.0f);
tileMap.TileSetPath = "assets/tilesets/terrain.png";

// Manipulate tiles
tileMap.SetTile(x: 5, y: 3, tileIndex: 12, layer: 0);
int tile = tileMap.GetTile(x: 5, y: 3, layer: 0);

// Layer management
tileMap.AddLayer("Decorations");
tileMap.RemoveLayer(1);
tileMap.ActiveLayerIndex = 0;

// Resize
tileMap.Resize(newWidth: 30, newHeight: 20);
```

### TileSet

```csharp
var tileSet = new TileSet
{
    TexturePath = "assets/tiles.png",
    TileWidth = 32,
    TileHeight = 32,
    Columns = 8,
    Rows = 8,
    Spacing = 0,
    Margin = 0
};
tileSet.LoadTexture();
tileSet.GenerateTiles();

// Get tile info
var tile = tileSet.GetTile(tileId: 5);
var (uvMin, uvMax) = tileSet.GetTileTextureCoords(tileId: 5);
```

## Best Practices

### Performance
- Use texture atlases instead of individual tile images
- Keep tilemap dimensions reasonable (< 100x100 for dense levels)
- Use layers sparingly (3-5 layers typical)
- Cache TileSets when possible

### Workflow
- Start with a single "Ground" layer
- Add "Walls", "Decorations", "Foreground" layers as needed
- Use Z-index to control rendering order
- Name layers descriptively

### Organization
- Store tilesets in `assets/tilesets/`
- Use consistent tile sizes across tilesets
- Document tile meanings (collision, decoration, etc.)

## Future Enhancements

- [ ] Tile auto-tiling / smart tiles
- [ ] Tile collision data editing
- [ ] Tile animation support
- [ ] Prefab tiles (composite tiles)
- [ ] Tile rotation and flipping
- [ ] Rule-based tile placement
- [ ] Minimap preview
- [ ] Export to common formats (TMX, JSON)
- [ ] Tileset editor with metadata
- [ ] Brush patterns and stamps

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Mouse Wheel | Zoom in/out |
| Middle Mouse | Pan canvas |
| Left Click | Paint/Erase tile |
| Esc | Close editor window |

## Troubleshooting

### Tiles not rendering
- Verify TileSet Path is correct
- Check Columns/Rows match your texture
- Ensure layer is visible
- Confirm entity has TransformComponent

### Wrong tiles displayed
- Recalculate Columns/Rows based on texture size
- Check for spacing/margin in tileset texture
- Verify tile indices are within range

### Performance issues
- Reduce tilemap dimensions
- Minimize visible layers
- Use smaller texture atlases
- Check for rendering system order

## Technical Details

### Memory Layout
- Tile data stored as `int[,]` (2D array)
- `-1` = empty tile
- `>= 0` = tile index in tileset
- Each layer maintains independent tile data

### Rendering
- Uses batched quad rendering via Graphics2D
- UV coordinates calculated from tileset configuration
- Supports texture tinting and opacity per layer
- Respects camera transform and viewport

### Coordinate System
- Grid coordinates: (0,0) at top-left
- World coordinates: Based on entity Transform + TileSize
- Screen coordinates: Editor viewport space

## Contributing

When extending the TileMap system:
1. Follow existing naming conventions
2. Update this documentation
3. Add unit tests for new features
4. Consider backward compatibility
5. Profile performance impact

---

**Created**: October 31, 2025  
**Version**: 1.0  
**Compatibility**: Engine v1.0+

