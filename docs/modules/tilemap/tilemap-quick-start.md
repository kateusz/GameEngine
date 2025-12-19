# TileMap Editor - Quick Start Guide

## 5-Minute Setup

### Step 1: Prepare Your Tileset
Create or obtain a tileset texture (PNG format):
- **Recommended size**: 256x256, 512x512, or 1024x1024
- **Tile size**: 16x16, 32x32, or 64x64 pixels
- **Layout**: Grid of tiles without spacing
- Save to your assets folder (e.g., `assets/tilesets/your_tileset.png`)

### Step 2: Create TileMap Entity
1. In Scene Hierarchy, right-click and create a new entity
2. Name it "TileMap"
3. In Properties panel, add the TileMap component

### Step 3: Configure TileMap
In the TileMap component properties, set:
- **Width/Height**: Map dimensions in tiles (e.g., 16x16)
- **Tile Size**: World units per tile (typically 1.0 x 1.0)
- **TileSet Path**: Path to your tileset texture
- **Columns/Rows**: Calculate from texture size divided by tile pixel size

### Step 4: Open Editor
Click the **"Open TileMap Editor"** button in the component properties.

### Step 5: Paint
1. Select a tile from the left palette (click on it)
2. Use tools to paint:
   - **Paint**: Left-click to place tiles
   - **Erase**: Remove tiles
   - **Fill**: Flood-fill an area
3. Pan: Right mouse button
4. Zoom: Mouse wheel

### Step 6: Transform Tiles (Optional)
Before placing tiles, you can apply transforms:
- **Rotate**: Press `R` or click "Rotate (R)" to rotate 90 degrees clockwise
- **Flip H**: Mirror tile horizontally
- **Flip V**: Mirror tile vertically
- **Reset**: Clear all transforms

The current transform state is shown in the toolbar.

## Common Operations

### Add a New Layer
1. In TileMap Editor, find the Layers section
2. Click "Add Layer"
3. Click the new layer to make it active
4. Paint on the new layer

### Hide/Show Layers
Click the checkbox next to the layer name.

### Change Tile Size
In Properties panel, adjust the "Tile Size" values.

### Resize TileMap
In Properties panel, change Width/Height values.

## Example Workflow: Creating a Small Room

1. Create TileMap entity and configure it (10x10 tiles, 1.0 tile size)
2. Load a dungeon tileset
3. Open the editor

**Layer 0 - "Floor":**
- Select a floor tile
- Use Fill tool to flood-fill the entire map

**Layer 1 - "Walls":**
- Add a new layer named "Walls"
- Select a wall tile
- Paint walls around the edges
- Use `R` to rotate corner pieces

**Layer 2 - "Decorations":**
- Add a new layer named "Decorations"
- Select decoration tiles (torches, etc.)
- Paint decorations on walls
- Use Flip H for symmetrical placement

Done! You have a multi-layered room with properly oriented tiles.

## Tips

**DO:**
- Start simple: single layer, small map
- Use descriptive layer names
- Keep tile size at 1.0 for pixel-perfect rendering
- Save scene frequently

**DON'T:**
- Make maps too large (keep under 100x100)
- Forget to set TileSet Path
- Paint without selecting a tile first
- Use too many layers (3-5 is ideal)

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Mouse Wheel | Zoom |
| Right Mouse | Pan |
| Left Click | Paint/Erase |
| R | Rotate tile 90 degrees clockwise |
| ESC | Close editor |

## Troubleshooting

**Can't see tiles?**
Check TileSet Path is correct and file exists.

**Wrong tiles shown?**
Verify Columns/Rows match your texture dimensions.

**Editor won't open?**
Make sure entity has TileMapComponent.

**Tiles too small/large?**
Adjust Tile Size in Properties panel.

---

Now you're ready to create levels! For advanced features, see the full documentation.
