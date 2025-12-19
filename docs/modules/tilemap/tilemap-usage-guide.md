# TileMap Usage Guide - From Editor to Game

## Overview

When you paint tiles in the editor, they're stored in the `TileMapComponent` on your entity. The `TileMapRenderSystem` automatically renders these tiles when you run the game.

## Using Your TileMap

### 1. Save Your Scene
Save your scene so the tilemap data is persisted. Use **Scene > Save** or **Ctrl+S**.

The scene file stores:
- Map dimensions (width, height)
- Tile size
- TileSet path and configuration
- All layers with their tiles (bit-packed format including rotation and flip data)

### 2. Run Your Game
When you play the scene, the TileMapRenderSystem automatically:
1. Loads the tileset texture
2. Creates SubTexture2D regions for each tile
3. Renders all tiles in each layer
4. Respects layer visibility and opacity

Click the **Play** button in the toolbar or use **Scene > Play**.

### 3. Positioning
- The TileMap entity's Transform determines where the tilemap appears
- Each tile is positioned relative to the entity transform
- TileSize property controls how large tiles appear in world units

## Component Properties

| Property | Description |
|----------|-------------|
| **Width** | Number of tiles horizontally |
| **Height** | Number of tiles vertically |
| **TileSize** | World-space size of each tile |
| **TileSetPath** | Path to spritesheet texture |
| **TileSetColumns** | Columns in spritesheet |
| **TileSetRows** | Rows in spritesheet |
| **Layers** | Collection of tile layers |

### Understanding TileSize

TileSize controls the **world-space size** of tiles:
- **(1.0, 1.0)**: Each tile is 1x1 world units (typical)
- **(2.0, 2.0)**: Each tile is 2x2 world units (larger)
- **(0.5, 0.5)**: Each tile is 0.5x0.5 world units (smaller)

## Working with Layers

### Layer Properties
Each layer has:
- **Name**: Identifier (e.g., "Ground", "Walls")
- **Visible**: Show/hide in editor and game
- **Opacity**: 0.0 (transparent) to 1.0 (opaque)
- **ZIndex**: Render order (higher = drawn on top)

### Layer Workflow
1. Click a layer to make it active
2. Paint tiles (only affects active layer)
3. Toggle visibility to see/hide layers
4. Use "Add Layer" to create new layers
5. Use "Remove Layer" to delete layers

At runtime, all visible layers render automatically, sorted by ZIndex.

## Programmatic Access

You can manipulate tilemaps from C# scripts using `TileMapComponent` methods:

### Key Operations
- **GetTile(x, y, layer)**: Returns TileData at position
- **SetTile(x, y, tileData, layer)**: Places a tile with optional rotation/flip
- **Layers[index]**: Access layer properties

### TileData
Each tile stores:
- **TileId**: Index in the tileset
- **Rotation**: None, 90, 180, or 270 degrees clockwise
- **FlipHorizontal / FlipVertical**: Mirror state
- **IsEmpty**: True if no tile at position

### Use Cases
- **Procedural generation**: Generate tiles with random rotations
- **Destructible terrain**: Remove tiles when destroyed
- **Dynamic levels**: Change tiles based on game state
- **Rotating platforms**: Animate tile rotation over time
- **Mirror puzzles**: Flip tiles to create reflected patterns
- **Auto-tiling**: Automatically rotate/flip tiles based on neighbors

## Common Workflows

### Creating a Level
1. Create TileMap entity
2. Configure tileset (path, columns, rows)
3. Open TileMap Editor
4. Paint ground layer
5. Add walls layer, paint walls
6. Add decorations layer, add details
7. Save scene
8. Test in Play mode

### Multiple Tilemaps
You can have multiple TileMap entities in one scene:
- **Background TileMap** at Z=-1: Environment behind player
- **Foreground TileMap** at Z=1: Decorative overlays in front
- **Collision TileMap** with opacity=0: Used only for collision detection

### Importing Existing Tilemap
If you have a tilemap from another tool (Tiled, etc.):
1. Export as image or data
2. Create TileMap entity
3. Set tileset to your spritesheet
4. Either paint manually or write a script to import data

## Performance Tips

**DO:**
- Use reasonable tilemap sizes (under 100x100 for dense levels)
- Keep layer count low (3-5 typical)
- Use layer visibility to hide unused layers
- Reuse tilesets across multiple tilemaps

**DON'T:**
- Create massive 1000x1000 tilemaps
- Use 20+ layers
- Load different textures for each tilemap unnecessarily

### Memory Usage
Each tile = 4 bytes. Even large tilemaps are lightweight:
- 16x16 tilemap = ~1 KB
- 100x100 tilemap with 3 layers = ~120 KB

## Troubleshooting

### Tiles Not Rendering
- Check TileSetPath is correct
- Verify Columns/Rows match your texture
- Ensure layer is Visible
- Check entity has TransformComponent
- Verify scene was saved

### Wrong Tiles Displayed
- Recalculate Columns/Rows (texture size / tile size)
- Check for spacing/margin in tileset
- Verify tile indices are correct
- Reload the scene

### Tiles Too Large/Small
- Adjust TileSize property (world units)
- Check camera zoom/position
- Verify entity Transform scale

## Quick Reference

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Save Scene | Ctrl+S |
| Rotate Tile | R |
| Zoom | Mouse Wheel |
| Pan | Right Mouse Button |
| Paint/Erase | Left Click |

### Transform Controls

| Control | Description |
|---------|-------------|
| Rotate (R) | Rotate tile 90 degrees clockwise |
| Flip H | Mirror tile horizontally |
| Flip V | Mirror tile vertically |
| Reset | Clear all transforms |

### Essential Workflow

1. Add TileMap component to entity
2. Configure tileset properties
3. Click "Open TileMap Editor"
4. Select tile from palette
5. (Optional) Apply rotation/flip transforms
6. Click/drag to paint on canvas
7. Use tools: Paint, Erase, Fill
8. Manage layers as needed
9. Close editor
10. Save scene (Ctrl+S)
11. Play and see your tiles!

---

**Related Documentation:**
- tilemap-quick-start.md - Quick start guide
- tilemap-tileset-configs.md - Tileset configuration examples
